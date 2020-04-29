using System.Threading.Tasks;
using Arcus.WebApi.Security.Authorization.Jwt;
using GuardNet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;

namespace Arcus.WebApi.Security.Authorization
{
    /// <summary>
    /// Authorization filter to verify if the HTTP request has a valid JWT token .
    /// </summary>
    public class JwtTokenAuthorizationFilter  : IAsyncAuthorizationFilter
    {
        /// <summary>
        /// Gets the default header name where the JWT token is expected in the HTTP request.
        /// </summary>
        public const string DefaultHeaderName = "x-managed-identity-token";

        private readonly IJwtTokenReader _jwtTokenReader;

        /// <summary>
        /// Initializes a new instance of the <see cref="JwtTokenAuthorizationFilter"/> class.
        /// </summary>
        /// <param name="jwtTokenReader">The instance to read the JWT from the HTTP request header.</param>
        public JwtTokenAuthorizationFilter (IJwtTokenReader jwtTokenReader)
        {
            Guard.NotNull(jwtTokenReader, nameof(jwtTokenReader));

            _jwtTokenReader = jwtTokenReader;
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
            if (context.HttpContext.Request.Headers.TryGetValue(DefaultHeaderName, out StringValues jwtString))
            {
                bool isValidToken = await _jwtTokenReader.IsValidTokenAsync(jwtString);
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
