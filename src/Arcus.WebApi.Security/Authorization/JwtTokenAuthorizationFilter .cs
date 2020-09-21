using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Arcus.WebApi.Security.Authorization.Jwt;
using GuardNet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
            Guard.For<ArgumentException>(() => context.HttpContext.Request == null, "Invalid action context given without any HTTP request");
            Guard.For<ArgumentException>(() => context.HttpContext.Request.Headers == null, "Invalid action context given without any HTTP request headers");
            Guard.For<ArgumentException>(() => context.HttpContext.RequestServices == null, "Invalid action context given without any HTTP request services");

            ILogger logger = context.HttpContext.RequestServices.GetLoggerOrDefault<JwtTokenAuthorizationFilter>();
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
                LogSecurityEvent(logger, SecurityResult.Failure, "No JWT MSI token was specified in the request");
                logger.LogError("No JWT MSI token was specified in the request, returning 401 Unauthorized");
                context.Result = new UnauthorizedObjectResult("No JWT MSI token header found in request");
            }
        }

        private static async Task ValidateJwtTokenAsync(IJwtTokenReader reader, AuthorizationFilterContext context, StringValues jwtString, ILogger logger)
        {
            if (String.IsNullOrWhiteSpace(jwtString))
            {
                LogSecurityEvent(logger, SecurityResult.Failure, "Cannot validate JWT MSI token because the token is blank");
                logger.LogError("Cannot validate JWT MSI token because the token is blank, returning 401 Unauthorized");
                context.Result = new UnauthorizedObjectResult("Blank JWT MSI token");
                
                return;
            }

            if (!JwtRegex.IsMatch(jwtString))
            {
                LogSecurityEvent(logger, SecurityResult.Failure, "Cannot validate JWT MSI token because the token is in an invalid format");
                logger.LogError("Cannot validate JWT MSI token because the token is in an invalid format, returning 401 Unauthorized");
                context.Result = new UnauthorizedObjectResult("Invalid JWT MSI token format");
                
                return;
            }

            bool isValidToken = await reader.IsValidTokenAsync(jwtString);
            if (isValidToken)
            {
                LogSecurityEvent(logger, SecurityResult.Success, "JWT MSI token is valid");
                logger.LogTrace("JWT MSI token is valid");
            }
            else
            {
                LogSecurityEvent(logger, SecurityResult.Failure, "Invalid JWT MSI token");
                logger.LogError("JWT MSI token is invalid, returning 401 Unauthorized");
                context.Result = new UnauthorizedObjectResult("Wrong JWT MSI token");
            }
        }

        private static void LogSecurityEvent(ILogger logger, SecurityResult result, string message)
        {
            /* TODO: use 'Arcus.Observability.Telemetry.Core' 'LogSecurityEvent' instead once the SQL dependency is moved
                       -> https://github.com/arcus-azure/arcus.observability/issues/131 */
            logger.LogInformation("Events {EventName} (Context: {@EventContext})", "Authorization", new Dictionary<string, object>
            {
                ["EventType"] = "Security",
                ["AuthorizationType"] = "JWT",
                ["Result"] = result.ToString(),
                ["Description"] = message
            });
        }
    }
}
