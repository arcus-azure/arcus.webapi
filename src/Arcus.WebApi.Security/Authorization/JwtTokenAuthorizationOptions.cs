using GuardNet;

namespace Arcus.WebApi.Security.Authorization
{
    public class JwtTokenAuthorizationOptions
    {
        public const string DefaultHeaderName = "x-identity-token";

        /// <summary>
        /// Gets the header name where the JWT token is expected in the HTTP request.
        /// </summary>
        public string HeaderName { get; set; }

        public JwtTokenAuthorizationOptions()
            : this(DefaultHeaderName)
        {
        }

        public JwtTokenAuthorizationOptions(string headerName)
        {
            Guard.NotNullOrEmpty(headerName, nameof(headerName));

            HeaderName = headerName;
        }
    }
}
