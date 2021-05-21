﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Arcus.Security.Core;
using Arcus.Security.Core.Caching;
using GuardNet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Arcus.WebApi.Security.Authentication.SharedAccessKey
{
    /// <summary>
    /// Authentication filter to secure HTTP requests with shared access keys.
    /// </summary>
    /// <remarks>
    ///     Please provide an <see cref="ISecretProvider"/> implementation in the configured services of the request.
    /// </remarks>
    public class SharedAccessKeyAuthenticationFilter : IAsyncAuthorizationFilter
    {
        private readonly string _headerName, _queryParameterName, _secretName;
        private readonly SharedAccessKeyAuthenticationOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="SharedAccessKeyAuthenticationFilter"/> class.
        /// </summary>
        /// <param name="headerName">The name of the request header which value must match the stored secret.</param>
        /// <param name="queryParameterName">The name of the query parameter which value must match the stored secret.</param>
        /// <param name="secretName">The name of the secret that's being retrieved using the <see cref="ISecretProvider.GetRawSecretAsync"/> call.</param>
        /// <exception cref="ArgumentException">Thrown when the both <paramref name="headerName"/> and <paramref name="queryParameterName"/> are blank.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="secretName"/> is blank.</exception>
        public SharedAccessKeyAuthenticationFilter(string headerName, string queryParameterName, string secretName)
            : this(headerName, queryParameterName, secretName, new SharedAccessKeyAuthenticationOptions())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SharedAccessKeyAuthenticationFilter"/> class.
        /// </summary>
        /// <param name="headerName">The name of the request header which value must match the stored secret.</param>
        /// <param name="queryParameterName">The name of the query parameter which value must match the stored secret.</param>
        /// <param name="secretName">The name of the secret that's being retrieved using the <see cref="ISecretProvider.GetRawSecretAsync"/> call.</param>
        /// <param name="options">The flag indicating whether or not this authentication filter should emit security events during processing of requests.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="secretName"/> is blank.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="headerName"/> and <paramref name="queryParameterName"/> are blank.</exception>
        public SharedAccessKeyAuthenticationFilter(string headerName, string queryParameterName, string secretName, SharedAccessKeyAuthenticationOptions options)
        {
            Guard.NotNullOrWhitespace(secretName, nameof(secretName), "Requires a non-blank secret name");
            Guard.For<ArgumentException>(
                () => String.IsNullOrWhiteSpace(headerName) && String.IsNullOrWhiteSpace(queryParameterName), 
                "Requires either a non-blank header name or query parameter name");

            _headerName = headerName;
            _queryParameterName = queryParameterName;
            _secretName = secretName;
            _options = options;
        }

        /// <summary>
        /// Called early in the filter pipeline to confirm request is authorized.
        /// </summary>
        /// <param name="context">The <see cref="T:Microsoft.AspNetCore.Mvc.Filters.AuthorizationFilterContext" />.</param>
        /// <returns>
        ///     A <see cref="T:System.Threading.Tasks.Task" /> that on completion indicates the filter has executed.
        /// </returns>
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            Guard.NotNull(context, nameof(context));
            Guard.NotNull(context.HttpContext, nameof(context.HttpContext));
            Guard.For<ArgumentException>(() => context.HttpContext.Request is null, "Invalid action context given without any HTTP request");
            Guard.For<ArgumentException>(() => context.HttpContext.Request.Headers is null, "Invalid action context given without any HTTP request headers");
            Guard.For<ArgumentException>(() => context.HttpContext.RequestServices is null, "Invalid action context given without any HTTP request services");

            ILogger logger = context.HttpContext.RequestServices.GetLoggerOrDefault<SharedAccessKeyAuthenticationFilter>();
            
            if (context.ActionDescriptor?.EndpointMetadata?.Any(m => m is BypassSharedAccessKeyAuthenticationAttribute || m is AllowAnonymousAttribute) == true)
            {
                logger.LogTrace("Bypass shared access key authentication because the '{SpecificAttribute}' or '{GeneralAttribute}' was found", nameof(BypassSharedAccessKeyAuthenticationAttribute), nameof(AllowAnonymousAttribute));
                return;
            }
            
            string foundSecret = await GetAuthorizationSecretAsync(context);

            if (!context.HttpContext.Request.Headers.ContainsKey(_headerName) 
                && !context.HttpContext.Request.Query.ContainsKey(_queryParameterName))
            {
                LogSecurityEvent(logger, $"Cannot verify shared access key because neither a request header '{_headerName}' or query parameter '{_queryParameterName}' was found in the incoming request that was configured for shared access authentication", HttpStatusCode.Unauthorized);
                context.Result = new UnauthorizedResult();
            }
            else
            {
                ValidateSharedAccessKeyInRequestHeader(context, foundSecret, logger);
                ValidateSharedAccessKeyInQueryParameter(context, foundSecret, logger);
            }
        }

        private async Task<string> GetAuthorizationSecretAsync(AuthorizationFilterContext context)
        {
            ISecretProvider userDefinedSecretProvider =
                context.HttpContext.RequestServices.GetService<ICachedSecretProvider>()
                ?? context.HttpContext.RequestServices.GetService<ISecretProvider>();

            if (userDefinedSecretProvider is null)
            {
                throw new KeyNotFoundException(
                    $"No configured {nameof(ICachedSecretProvider)} or {nameof(ISecretProvider)} implementation found in the request service container. "
                    + "Please configure such an implementation (ex. in the Startup) of your application");
            }

            Task<string> rawSecretAsync = userDefinedSecretProvider.GetRawSecretAsync(_secretName);
            if (rawSecretAsync is null)
            {
                throw new InvalidOperationException(
                    $"Configured {nameof(ISecretProvider)} is not implemented correctly as it returns 'null' for a {nameof(Task)} value when calling {nameof(ISecretProvider.GetRawSecretAsync)}");
            }

            string foundSecret = await rawSecretAsync;
            if (foundSecret is null)
            {
                throw new SecretNotFoundException(_secretName);
            }

            return foundSecret;
        }

        private void ValidateSharedAccessKeyInRequestHeader(AuthorizationFilterContext context, string foundSecret, ILogger logger)
        {
            if (String.IsNullOrWhiteSpace(_headerName))
            {
                return;
            }

            if (context.HttpContext.Request.Headers.TryGetValue(_headerName, out StringValues requestSecretHeaders))
            {
                if (requestSecretHeaders.Any(headerValue => headerValue != foundSecret))
                {
                    LogSecurityEvent(logger, $"Shared access key in request header '{_headerName}' doesn't match expected access key", HttpStatusCode.Unauthorized);
                    context.Result = new UnauthorizedObjectResult("Shared access key in request doesn't match expected access key");
                }
                else
                {
                    LogSecurityEvent(logger, $"Shared access key in request header '{_headerName}' matches expected access key");
                }
            }
            else
            {
                LogSecurityEvent(logger, $"No shared access key found in request header '{_headerName}'", HttpStatusCode.Unauthorized);
                context.Result = new UnauthorizedObjectResult("No shared access key found in request");
            }
        }

        private void ValidateSharedAccessKeyInQueryParameter(AuthorizationFilterContext context, string foundSecret, ILogger logger)
        {
            if (String.IsNullOrWhiteSpace(_queryParameterName))
            {
                return;
            }

            if (context.HttpContext.Request.Query.ContainsKey(_queryParameterName))
            {
                if (context.HttpContext.Request.Query[_queryParameterName] != foundSecret)
                {
                    LogSecurityEvent(logger, $"Shared access key in query parameter '{_queryParameterName}' doesn't match expected access key", HttpStatusCode.Unauthorized);
                    context.Result = new UnauthorizedObjectResult("Shared access key in request doesn't match expected access key");
                }
                else
                {
                    LogSecurityEvent(logger, $"Shared access key in query parameter '{_queryParameterName}' matches expected access key");
                }
            }
            else
            {
                LogSecurityEvent(logger, $"No shared access key found in query parameter '{_queryParameterName}'", HttpStatusCode.Unauthorized);
                context.Result = new UnauthorizedObjectResult("No shared access key found in request");
            }
        }

        private void LogSecurityEvent(ILogger logger, string description, HttpStatusCode? responseStatusCode = null)
        {
            if (!_options.EmitSecurityEvents)
            {
                return;
            }
            
            var telemetryContext = new Dictionary<string, object>
            {
                ["EventType"] = "Security",
                ["AuthenticationType"] = "Shared access key",
                ["Description"] = description
            };

            if (responseStatusCode != null)
            {
                telemetryContext["StatusCode"] = responseStatusCode.ToString();
            }

            logger.LogSecurityEvent("Authentication", telemetryContext);
        }
    }
}
