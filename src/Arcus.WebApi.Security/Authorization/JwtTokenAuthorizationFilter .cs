using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using Arcus.WebApi.Security.Authorization.Jwt;
using GuardNet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Arcus.WebApi.Security.Authorization
{
    /// <summary>
    /// Authorization filter to verify if the HTTP request has a valid JWT token .
    /// </summary>
    public class JwtTokenAuthorizationFilter  : IAsyncAuthorizationFilter
    {
        private const string JwtPattern = "^(Bearer )?[A-Za-z0-9_-]+\\.[A-Za-z0-9_-]+\\.[A-Za-z0-9_-]+$";
        private static readonly Regex JwtRegex = new Regex(JwtPattern, RegexOptions.Compiled);

        private readonly JwtTokenAuthorizationOptions _authorizationOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="JwtTokenAuthorizationFilter"/> class.
        /// </summary>
        /// <param name="authorizationOptions">The options for configuring how to authorize requests.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="authorizationOptions"/> is <c>null</c>.</exception>
        public JwtTokenAuthorizationFilter(JwtTokenAuthorizationOptions authorizationOptions)
        {
            Guard.NotNull(authorizationOptions, nameof(authorizationOptions), 
                "Requires a set of options to configure how the JWT authorization filter should authorize requests");

            _authorizationOptions = authorizationOptions;
        }

        /// <summary>
        /// Called early in the filter pipeline to confirm request is authorized.
        /// </summary>
        /// <param name="context">The <see cref="T:Microsoft.AspNetCore.Mvc.Filters.AuthorizationFilterContext" />.</param>
        /// <returns>
        ///     A <see cref="T:System.Threading.Tasks.Task" /> that on completion indicates the filter has executed.
        /// </returns>
        public virtual async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            Guard.NotNull(context, nameof(context));
            Guard.NotNull(context.HttpContext, nameof(context.HttpContext));
            Guard.For<ArgumentException>(() => context.HttpContext.Request is null, "Invalid action context given without any HTTP request");
            Guard.For<ArgumentException>(() => context.HttpContext.Request.Headers is null, "Invalid action context given without any HTTP request headers");
            Guard.For<ArgumentException>(() => context.HttpContext.RequestServices is null, "Invalid action context given without any HTTP request services");

            ILogger logger = context.HttpContext.RequestServices.GetLoggerOrDefault<JwtTokenAuthorizationFilter>();
            
            if (context.ActionDescriptor?.EndpointMetadata?.Any(m => m is BypassJwtTokenAuthorizationAttribute || m is AllowAnonymousAttribute) == true)
            {
                logger.LogTrace("Bypass JWT authorization on this path because the '{SpecificAttribute}' of '{GeneralAttribute}' was found", nameof(BypassJwtTokenAuthorizationAttribute), nameof(AllowAnonymousAttribute));
                return;
            }
            
            IJwtTokenReader reader = _authorizationOptions.GetOrCreateJwtTokenReader(context.HttpContext.RequestServices);
            if (reader is null)
            {
                logger.LogError("Cannot validate JWT MSI token because no '{Type}' was registered in the options of the JWT authorization filter", nameof(IJwtTokenReader));
                throw new InvalidOperationException("Cannot validate JWT MSI token because the registered JWT options were invalid");
            }

            if (context.HttpContext.Request.Headers.TryGetValue(_authorizationOptions.HeaderName, out StringValues jwtString))
            {
                await ValidateJwtTokenAsync(reader, context, jwtString, logger);
            }
            else
            {
                LogSecurityEvent(logger, "No JWT MSI token was specified in the request", HttpStatusCode.Unauthorized);
                context.Result = new UnauthorizedObjectResult("No JWT MSI token header found in request");
            }
        }

        private static async Task ValidateJwtTokenAsync(IJwtTokenReader reader, AuthorizationFilterContext context, StringValues jwtString, ILogger logger)
        {
            if (String.IsNullOrWhiteSpace(jwtString))
            {
                LogSecurityEvent(logger, "Cannot validate JWT MSI token because the token is blank", HttpStatusCode.Unauthorized);
                context.Result = new UnauthorizedObjectResult("Blank JWT MSI token");
                
                return;
            }

            if (!JwtRegex.IsMatch(jwtString))
            {
                LogSecurityEvent(logger, "Cannot validate JWT MSI token because the token is in an invalid format", HttpStatusCode.Unauthorized);
                context.Result = new UnauthorizedObjectResult("Invalid JWT MSI token format");
                
                return;
            }

            bool isValidToken = await reader.IsValidTokenAsync(jwtString);
            if (isValidToken)
            {
                LogSecurityEvent(logger, "JWT MSI token is valid");
            }
            else
            {
                LogSecurityEvent(logger, "JWT MSI token is invalid", HttpStatusCode.Unauthorized);
                context.Result = new UnauthorizedObjectResult("Wrong JWT MSI token");
            }
        }

        private static void LogSecurityEvent(ILogger logger, string description, HttpStatusCode? responseStatusCode = null)
        {
            var telemetryContext = new Dictionary<string, object>
            {
                ["EventType"] = "Security",
                ["AuthorizationType"] = "JWT",
                ["Description"] = description
            };

            if (responseStatusCode != null)
            {
                telemetryContext["StatusCode"] = responseStatusCode.ToString();
            }

            logger.LogSecurityEvent("Authorization", telemetryContext);
        }
    }
}
