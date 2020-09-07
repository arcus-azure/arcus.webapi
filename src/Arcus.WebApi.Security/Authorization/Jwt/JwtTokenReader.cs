using System;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using GuardNet;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Arcus.WebApi.Security.Authorization.Jwt
{
    /// <summary>
    /// Represents an <see cref="IJwtTokenReader"/> implementation to verify with OpenID configuration the JWT token from the HTTP request header.
    /// </summary>
    public class JwtTokenReader : IJwtTokenReader
    {
        private const string MicrosoftDiscoveryEndpoint =
            "https://login.microsoftonline.com/common/v2.0/.well-known/openid-configuration";

        private readonly string _applicationId;
        private readonly TokenValidationParameters _tokenValidationParameters;
        private readonly JwtSecurityTokenHandler _handler = new JwtSecurityTokenHandler();
        private readonly ConfigurationManager<OpenIdConnectConfiguration> _configManager;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="JwtTokenReader"/> class.
        /// </summary>
        /// <remarks>Uses Microsoft OpenId connect discovery endpoint</remarks>
        /// <param name="applicationId">The Azure AD Application used as audience to validate against.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="applicationId"/> is blank.</exception>
        public JwtTokenReader(string applicationId) : this(applicationId, NullLogger<JwtTokenReader>.Instance)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JwtTokenReader"/> class.
        /// </summary>
        /// <remarks>Uses Microsoft OpenId connect discovery endpoint</remarks>
        /// <param name="applicationId">The Azure AD Application used as audience to validate against.</param>
        /// <param name="logger">The logger instance to write diagnostic messages when verifying the JWT token.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="applicationId"/> is blank.</exception>
        public JwtTokenReader(string applicationId, ILogger<JwtTokenReader> logger)
        {
            Guard.NotNullOrWhitespace(applicationId, nameof(applicationId), "Requires an Azure D application ID used as audience to validate against.");

            _applicationId = applicationId;
            _logger = logger ?? NullLogger<JwtTokenReader>.Instance;
            _configManager = new ConfigurationManager<OpenIdConnectConfiguration>(MicrosoftDiscoveryEndpoint, new OpenIdConnectConfigurationRetriever());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JwtTokenReader"/> class.
        /// </summary>
        /// <remarks>Uses Microsoft OpenId connect discovery endpoint</remarks>
        /// <param name="tokenValidationParameters">The collection of parameters to influence how the token validation is done.</param>
        public JwtTokenReader(TokenValidationParameters tokenValidationParameters) 
            : this(tokenValidationParameters, MicrosoftDiscoveryEndpoint, NullLogger<JwtTokenReader>.Instance)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JwtTokenReader"/> class.
        /// </summary>
        /// <remarks>Uses Microsoft OpenId connect discovery endpoint</remarks>
        /// <param name="tokenValidationParameters">The collection of parameters to influence how the token validation is done.</param>
        /// <param name="logger">The logger instance to write diagnostic messages when verifying the JWT token.</param>
        public JwtTokenReader(TokenValidationParameters tokenValidationParameters, ILogger<JwtTokenReader> logger)
            : this(tokenValidationParameters, MicrosoftDiscoveryEndpoint, logger)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JwtTokenReader"/> class.
        /// </summary>
        /// <param name="tokenValidationParameters">The collection of parameters to influence how the token validation is done.</param>
        /// <param name="openIdConnectDiscoveryUri">The URI of an OpenId connect endpoint for discovering the OpenId configuration.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="tokenValidationParameters"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="openIdConnectDiscoveryUri"/> is blank.</exception>
        /// <summary>
        /// Initializes a new instance of the <see cref="JwtTokenReader"/> class.
        /// </summary>
        public JwtTokenReader(TokenValidationParameters tokenValidationParameters, string openIdConnectDiscoveryUri)
            : this(tokenValidationParameters, openIdConnectDiscoveryUri, NullLogger<JwtTokenReader>.Instance)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JwtTokenReader"/> class.
        /// </summary>
        /// <param name="tokenValidationParameters">The collection of parameters to influence how the token validation is done.</param>
        /// <param name="openIdConnectDiscoveryUri">The URI of an OpenId connect endpoint for discovering the OpenId configuration.</param>
        /// <param name="logger">The logger instance to write diagnostic messages when verifying the JWT token.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="tokenValidationParameters"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="openIdConnectDiscoveryUri"/> is blank.</exception>
        public JwtTokenReader(TokenValidationParameters tokenValidationParameters, string openIdConnectDiscoveryUri, ILogger<JwtTokenReader> logger)
        {
            Guard.NotNull(tokenValidationParameters, nameof(tokenValidationParameters), "Requires a collection of parameters to influence how the token validation is done");
            Guard.NotNullOrWhitespace(openIdConnectDiscoveryUri, nameof(openIdConnectDiscoveryUri), "Requires an non-blank OpenId URI connection endpoint for discovering the OpenId configuration");

            _tokenValidationParameters = tokenValidationParameters;
            _logger = logger ?? NullLogger<JwtTokenReader>.Instance;
            _configManager = new ConfigurationManager<OpenIdConnectConfiguration>(openIdConnectDiscoveryUri, new OpenIdConnectConfigurationRetriever());
        }

        /// <summary>
        ///     Verify if the token is considered valid.
        /// </summary>
        /// <param name="token">The JWT token.</param>
        public async Task<bool> IsValidTokenAsync(string token)
        {
            try
            {
                _logger.LogTrace("Verifying request JWT...");
                TokenValidationParameters validationParameters = await DetermineTokenValidationParametersAsync();
                _handler.ValidateToken(token, validationParameters, out SecurityToken jwtToken);
                _logger.LogTrace("Request JWT is considered valid!");
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Unexpected failure during verifying the JWT");
                return false;
            }

            return true;
        }

        private async Task<TokenValidationParameters> DetermineTokenValidationParametersAsync()
        {
            if (_tokenValidationParameters is null)
            {
                OpenIdConnectConfiguration config = await _configManager.GetConfigurationAsync();
                
                var validationParameters = new TokenValidationParameters
                {
                    ValidateAudience = false,
                    ValidAudience = _applicationId,
                    ValidateIssuer = false,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKeys = config.SigningKeys,
                    ValidateLifetime = true
                };

                return validationParameters;
            }

            return _tokenValidationParameters;
        }
    }
}