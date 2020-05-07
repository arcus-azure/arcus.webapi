using Arcus.WebApi.Security.Authorization.Jwt;
using GuardNet;
using Microsoft.IdentityModel.Tokens;

namespace Arcus.WebApi.Security.Authorization
{
    public class JwtTokenAuthorizationOptions
    {
        public const string DefaultHeaderName = "x-identity-token";

        private IJwtTokenReader _jwtTokenReader;

        /// <summary>
        /// Gets or sets the header name where the JWT token is expected in the HTTP request.
        /// </summary>
        public string HeaderName { get; set; }

        /// <summary>
        /// Gets or sets the JSON web token reader to verify the token from the HTTP request header.
        /// </summary>
        public IJwtTokenReader JwtTokenReader
        {
            get => _jwtTokenReader;
            set
            {
                Guard.NotNull(value, nameof(value));
                _jwtTokenReader = value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JwtTokenAuthorizationOptions"/> class.
        /// </summary>
        public JwtTokenAuthorizationOptions()
            : this(new JwtTokenReader(new TokenValidationParameters()))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JwtTokenAuthorizationOptions"/> class.
        /// </summary>
        /// <param name="reader">The JWT reader to verify the token from the HTTP request header.</param>
        public JwtTokenAuthorizationOptions(IJwtTokenReader reader)
            : this(reader, DefaultHeaderName)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JwtTokenAuthorizationOptions"/> class.
        /// </summary>
        /// <param name="reader">The JWT reader to verify the token from the HTTP request header.</param>
        /// <param name="headerName">The name of the header where the JWT token is expected.</param>
        public JwtTokenAuthorizationOptions(IJwtTokenReader reader, string headerName)
        {
            Guard.NotNull(reader, nameof(reader));
            Guard.NotNullOrEmpty(headerName, nameof(headerName));

            JwtTokenReader = reader;
            HeaderName = headerName;
        }
    }
}
