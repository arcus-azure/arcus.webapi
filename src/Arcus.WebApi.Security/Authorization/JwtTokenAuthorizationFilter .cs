using System.Threading.Tasks;
using GuardNet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Arcus.WebApi.Security.Authorization
{
    /// <summary>
    /// Authorization filter to verify if the HTTP request has a valid JWT token .
    /// </summary>
    public class JwtTokenAuthorizationFilter  : IAsyncAuthorizationFilter
    {
        private readonly JwtTokenAuthorizationOptions _authorizationOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="JwtTokenAuthorizationFilter"/> class.
        /// </summary>
        /// <param name="authorizationOptions">Options for configuring how to authorize requests</param>
        public JwtTokenAuthorizationFilter(JwtTokenAuthorizationOptions authorizationOptions)
        {
            Guard.NotNull(authorizationOptions, nameof(authorizationOptions));

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
            if (context.HttpContext.Request.Headers.TryGetValue(_authorizationOptions.HeaderName, out StringValues jwtString))
            {
                bool isValidToken = await _authorizationOptions.JwtTokenReader.IsValidTokenAsync(jwtString);
                if (string.IsNullOrWhiteSpace(jwtString) || isValidToken == false)
                {
                    context.Result = new UnauthorizedObjectResult("Unauthorized because of wrong JWT MSI token.");
                }
            }
            else
            {
                context.Result = new UnauthorizedObjectResult("Unauthorized because of missing JWT MSI token header.");
            }
        }
    }
}
