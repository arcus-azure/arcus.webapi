using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Arcus.Security.Core;
using Arcus.Security.Core.Caching;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
        /// <param name="options">The set of additional consumer-configurable options to change the behavior of the shared access authentication.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="secretName"/> is blank.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="headerName"/> and <paramref name="queryParameterName"/> are blank.</exception>
        public SharedAccessKeyAuthenticationFilter(string headerName, string queryParameterName, string secretName, SharedAccessKeyAuthenticationOptions options)
        {
            if (string.IsNullOrWhiteSpace(secretName))
            {
                throw new ArgumentException("Requires a non-blank secret name", nameof(secretName));
            }

            if (string.IsNullOrWhiteSpace(headerName) && string.IsNullOrWhiteSpace(queryParameterName))
            {
                throw new ArgumentException("Requires either a non-blank header name or query parameter name");
            }

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
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.HttpContext is null)
            {
                throw new ArgumentNullException(nameof(context.HttpContext));
            }

            if (context.HttpContext.Request is null)
            {
                throw new ArgumentException("Invalid action context given without any HTTP request");
            }

            if (context.HttpContext.Request.Headers is null)
            {
                throw new ArgumentException("Invalid action context given without any HTTP request headers");
            }

            if (context.HttpContext.RequestServices is null)
            {
                throw new ArgumentException("Invalid action context given without any HTTP request services");
            }

            ILogger logger = context.HttpContext.RequestServices.GetLoggerOrDefault<SharedAccessKeyAuthenticationFilter>();
            
            if (context.ActionDescriptor?.EndpointMetadata?.Any(m => m is BypassSharedAccessKeyAuthenticationAttribute || m is AllowAnonymousAttribute) == true)
            {
                logger.LogTrace("Bypass shared access key authentication because the '{SpecificAttribute}' or '{GeneralAttribute}' was found", nameof(BypassSharedAccessKeyAuthenticationAttribute), nameof(AllowAnonymousAttribute));
                return;
            }
            
            string[] foundSecrets = await GetAuthorizationSecretAsync(context);

            HttpRequest request = context.HttpContext.Request;
            bool containsHeader = _headerName != null && request.Headers.ContainsKey(_headerName);
            bool containsQuery = _queryParameterName != null && request.Query.ContainsKey(_queryParameterName);

            if (containsHeader || containsQuery)
            {
                ValidateSharedAccessKeyInRequestHeader(context, foundSecrets, logger);
                ValidateSharedAccessKeyInQueryParameter(context, foundSecrets, logger);
            }
            else
            {
                LogSecurityEvent(logger, $"Cannot verify shared access key because neither a request header '{_headerName}' or query parameter '{_queryParameterName}' was found in the incoming request that was configured for shared access authentication", HttpStatusCode.Unauthorized);
                context.Result = new UnauthorizedResult();
            }
        }

        private async Task<string[]> GetAuthorizationSecretAsync(AuthorizationFilterContext context)
        {
            var userDefinedSecretProvider = context.HttpContext.RequestServices.GetService<ISecretProvider>() ?? throw new InvalidOperationException(
                    "Cannot retrieve the shared access key to validate the HTTP request because no Arcus secret store was registered in the application," 
                    + $"please register the secret store with '{nameof(IHostBuilderExtensions.ConfigureSecretStore)}' on the '{nameof(IHostBuilder)}' or with 'AddSecretStore' on the '{nameof(IServiceCollection)}'," 
                    + "for more information on the Arcus secret store: https://security.arcus-azure.net/features/secret-store");
            Task<IEnumerable<string>> rawSecretAsync = userDefinedSecretProvider.GetRawSecretsAsync(_secretName) ?? throw new InvalidOperationException(
                    $"Configured {nameof(ISecretProvider)} is not implemented correctly as it returns 'null' for a {nameof(Task)} value when calling {nameof(ISecretProvider.GetRawSecretAsync)}");
            IEnumerable<string> foundSecrets = await rawSecretAsync;
            return foundSecrets is null ? throw new SecretNotFoundException(_secretName) : foundSecrets.ToArray();
        }

        private void ValidateSharedAccessKeyInRequestHeader(AuthorizationFilterContext context, string[] foundSecrets, ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(_headerName))
            {
                return;
            }

            if (context.HttpContext.Request.Headers.TryGetValue(_headerName, out StringValues requestSecretHeaders))
            {
                if (foundSecrets.Any(secret => secret == requestSecretHeaders.ToString()))
                {
                    LogSecurityEvent(logger, $"Shared access key in request header '{_headerName}' matches expected access key");
                }
                else
                {
                    LogSecurityEvent(logger, $"Shared access key in request header '{_headerName}' doesn't match expected access key", HttpStatusCode.Unauthorized);
                    context.Result = new UnauthorizedObjectResult("Shared access key in request doesn't match expected access key");
                }
            }
            else
            {
                LogSecurityEvent(logger, $"No shared access key found in request header '{_headerName}'", HttpStatusCode.Unauthorized);
                context.Result = new UnauthorizedObjectResult("No shared access key found in request");
            }
        }

        private void ValidateSharedAccessKeyInQueryParameter(AuthorizationFilterContext context, string[] foundSecrets, ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(_queryParameterName))
            {
                return;
            }

            if (context.HttpContext.Request.Query.TryGetValue(_queryParameterName, out StringValues querySecretParameters))
            {
                if (foundSecrets.Any(secret => secret == querySecretParameters.ToString()))
                {
                    LogSecurityEvent(logger, $"Shared access key in query parameter '{_queryParameterName}' matches expected access key");
                }
                else
                {
                    LogSecurityEvent(logger, $"Shared access key in query parameter '{_queryParameterName}' doesn't match expected access key", HttpStatusCode.Unauthorized);
                    context.Result = new UnauthorizedObjectResult("Shared access key in request doesn't match expected access key");
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
