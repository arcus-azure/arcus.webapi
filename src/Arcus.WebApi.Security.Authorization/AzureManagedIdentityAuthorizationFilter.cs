using System.Threading.Tasks;
using Arcus.WebApi.Security.Authorization.Jwt;
using GuardNet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Arcus.WebApi.Security.Authorization
{
    public class AzureManagedIdentityAuthorizationFilter : IAsyncAuthorizationFilter
    {
        private readonly AzureManagedIdentityAuthorizationOptions _options;
        private readonly IJwtTokenReader _jwtTokenReader;

        public AzureManagedIdentityAuthorizationFilter(IJwtTokenReader jwtTokenReader, AzureManagedIdentityAuthorizationOptions options)
        {
            Guard.NotNull(options, nameof(options));
            Guard.NotNull(jwtTokenReader, nameof(jwtTokenReader));

            _options = options;
            _jwtTokenReader = jwtTokenReader;
        }

        public virtual async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var jwtString = context.HttpContext.Request.Headers[_options.HeaderName].ToString();
            var isValidToken = await _jwtTokenReader.IsValidTokenAsync(jwtString);

            if (string.IsNullOrWhiteSpace(jwtString) || isValidToken == false)
            {
                context.Result = new UnauthorizedObjectResult("Unauthorized because of wrong JWT MSI token.");
            }
        }
    }
}
