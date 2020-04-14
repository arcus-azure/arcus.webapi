using System.Threading.Tasks;
using Arcus.WebApi.Security.Authorization.Jwt;
using GuardNet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Arcus.WebApi.Security.Authorization
{
    public class AzureManagedIdentityAuthorizationFilter : IAsyncAuthorizationFilter
    {
        private const string DefaultHeaderName = "x-managed-identity-token";

        private readonly string _headerName;
        private readonly IJwtTokenReader _jwtTokenReader;

        public AzureManagedIdentityAuthorizationFilter(IJwtTokenReader jwtTokenReader)
            : this(DefaultHeaderName, jwtTokenReader)
        {
        }

        public AzureManagedIdentityAuthorizationFilter(string headerName, IJwtTokenReader jwtTokenReader)
        {
            Guard.NotNullOrEmpty(headerName, nameof(headerName));
            Guard.NotNull(jwtTokenReader, nameof(jwtTokenReader));

            _headerName = headerName;
            _jwtTokenReader = jwtTokenReader;
        }

        public virtual async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var jwtString = context.HttpContext.Request.Headers[_headerName].ToString();
            var isValidToken = await _jwtTokenReader.IsValidTokenAsync(jwtString);

            if (string.IsNullOrWhiteSpace(jwtString) || isValidToken == false)
            {
                context.Result = new UnauthorizedObjectResult("Unauthorized because of wrong JWT MSI token.");
            }
        }
    }
}
