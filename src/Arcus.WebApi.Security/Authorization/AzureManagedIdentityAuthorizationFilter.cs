using System;
using System.Threading.Tasks;
using Arcus.WebApi.Security.Authorization.Jwt;
using GuardNet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
#pragma warning disable 1591 // Ignore public XML code docs warnings.

namespace Arcus.WebApi.Security.Authorization
{
   [Obsolete("Renamed to " + nameof(JwtTokenAuthorizationFilter))]
    public class AzureManagedIdentityAuthorizationFilter : IAsyncAuthorizationFilter
    {
        public const string DefaultHeaderName = "x-managed-identity-token";

        private readonly string _headerName;
        private readonly IJwtTokenReader _jwtTokenReader;

        public AzureManagedIdentityAuthorizationFilter(IJwtTokenReader jwtTokenReader)
        {
            Guard.NotNull(jwtTokenReader, nameof(jwtTokenReader));

            _headerName = DefaultHeaderName;
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