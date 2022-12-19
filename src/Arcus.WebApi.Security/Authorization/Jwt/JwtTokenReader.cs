using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using GuardNet;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using IdentityModel;
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

        private readonly IDictionary<string, string> _claimCheck;
        private readonly TokenValidationParameters _tokenValidationParameters;
        private readonly JwtSecurityTokenHandler _handler = new JwtSecurityTokenHandler();
        private readonly ConfigurationManager<OpenIdConnectConfiguration> _configManager;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="JwtTokenReader"/> class.
        /// </summary>
        /// <remarks>Uses Microsoft OpenId connect discovery endpoint</remarks>
        /// <param name="audience">The id of the intended audience for which this token must be validated against.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="audience"/> is blank.</exception>
        public JwtTokenReader(string audience) : this(audience, NullLogger<JwtTokenReader>.Instance)
        {
            Guard.NotNullOrWhitespace(audience, nameof(audience), "Requires an audience to validate against");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JwtTokenReader"/> class.
        /// </summary>
        /// <remarks>Uses Microsoft OpenId connect discovery endpoint</remarks>
        /// <param name="audience">The id of the intended audience for which this token must be validated against.</param>
        /// <param name="logger">The logger instance to write diagnostic messages when verifying the JWT token.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="audience"/> is blank.</exception>
        public JwtTokenReader(string audience, ILogger<JwtTokenReader> logger) : this(new Dictionary<string, string> {{ JwtClaimTypes.Audience, audience}}, logger)
        {
            Guard.NotNullOrWhitespace(audience, nameof(audience), "Requires an audience to validate against");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JwtTokenReader"/> class.
        /// </summary>
        /// <remarks>Uses Microsoft OpenId connect discovery endpoint</remarks>
        /// <param name="claimCheck">A dictionary that contains key/value pairs representing the JWT claims that must be checked.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="claimCheck"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="claimCheck"/> doesn't have any entries or one of the entries has blank key/value inputs.</exception>
        public JwtTokenReader(IDictionary<string, string> claimCheck) : this(claimCheck, NullLogger<JwtTokenReader>.Instance)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JwtTokenReader"/> class.
        /// </summary>
        /// <remarks>Uses Microsoft OpenId connect discovery endpoint</remarks>
        /// <param name="claimCheck">A dictionary that contains key/value pairs representing the JWT claims that must be checked.</param>
        /// <param name="logger">The logger instance to write diagnostic messages when verifying the JWT token.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="claimCheck"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="claimCheck"/> doesn't have any entries or one of the entries has blank key/value inputs.</exception>
        public JwtTokenReader(IDictionary<string, string> claimCheck, ILogger<JwtTokenReader> logger)
        {
            Guard.NotNull(claimCheck, nameof(claimCheck), "Requires a set of claim checks to verify the claims request JWT");
            Guard.NotAny(claimCheck, nameof(claimCheck), "Requires at least one entry in the set of claim checks to verify the claims in the request JWT");
            Guard.For<ArgumentException>(() => claimCheck.Any(item => String.IsNullOrWhiteSpace(item.Key) || String.IsNullOrWhiteSpace(item.Value)), 
                "Requires all entries in the set of claim checks to be non-blank to correctly verify the claims in the request JWT");

            _claimCheck = claimCheck;
            _logger = logger ?? NullLogger<JwtTokenReader>.Instance;
            _configManager = new ConfigurationManager<OpenIdConnectConfiguration>(MicrosoftDiscoveryEndpoint, new OpenIdConnectConfigurationRetriever());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JwtTokenReader"/> class.
        /// </summary>
        /// <remarks>Uses Microsoft OpenId connect discovery endpoint</remarks>
        /// <param name="tokenValidationParameters">The collection of parameters to influence how the token validation is done.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="tokenValidationParameters"/> is <c>null</c>.</exception>
        public JwtTokenReader(TokenValidationParameters tokenValidationParameters) 
            : this(tokenValidationParameters, MicrosoftDiscoveryEndpoint, NullLogger<JwtTokenReader>.Instance)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JwtTokenReader"/> class.
        /// </summary>
        /// <remarks>Uses Microsoft OpenId connect discovery endpoint</remarks>
        /// <param name="tokenValidationParameters">Collection of parameters to influence how the token validation is done</param>
        /// <param name="claimCheck">Custom claims key-value pair to validate against</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="tokenValidationParameters"/> or the <paramref name="claimCheck"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="claimCheck"/> doesn't have any entries or one of the entries has blank key/value inputs.</exception>
        public JwtTokenReader(TokenValidationParameters tokenValidationParameters, IDictionary<string, string> claimCheck) 
            : this(tokenValidationParameters, MicrosoftDiscoveryEndpoint, claimCheck)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JwtTokenReader"/> class.
        /// </summary>
        /// <remarks>Uses Microsoft OpenId connect discovery endpoint</remarks>
        /// <param name="tokenValidationParameters">Collection of parameters to influence how the token validation is done</param>
        /// <param name="claimCheck">Custom claims key-value pair to validate against</param>
        /// <param name="logger">The logger instance to write diagnostic messages when verifying the JWT token.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="tokenValidationParameters"/> or the <paramref name="claimCheck"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="claimCheck"/> doesn't have any entries or one of the entries has blank key/value inputs.</exception>
        public JwtTokenReader(TokenValidationParameters tokenValidationParameters, IDictionary<string, string> claimCheck, ILogger<JwtTokenReader> logger) 
            : this(tokenValidationParameters, MicrosoftDiscoveryEndpoint, claimCheck, logger)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JwtTokenReader"/> class.
        /// </summary>
        /// <remarks>Uses Microsoft OpenId connect discovery endpoint</remarks>
        /// <param name="tokenValidationParameters">The collection of parameters to influence how the token validation is done.</param>
        /// <param name="logger">The logger instance to write diagnostic messages when verifying the JWT token.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="tokenValidationParameters"/> is <c>null</c>.</exception>
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
        /// Initializes a new instance of the <see cref="JwtTokenReader"/> class.
        /// </summary>
        /// <param name="openIdConnectDiscoveryUri">Uri of an OpenId connect endpoint for discovering the configuration</param>
        /// <param name="tokenValidationParameters">Collection of parameters to influence how the token validation is done</param>
        /// <param name="claimCheck">Custom claims key-value pair to validate against</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="claimCheck"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="claimCheck"/> doesn't have any entries or one of the entries has blank key/value inputs.</exception>
        public JwtTokenReader(TokenValidationParameters tokenValidationParameters, string openIdConnectDiscoveryUri, IDictionary<string, string> claimCheck)
            : this(tokenValidationParameters, openIdConnectDiscoveryUri, claimCheck, NullLogger<JwtTokenReader>.Instance)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JwtTokenReader"/> class.
        /// </summary>
        /// <param name="openIdConnectDiscoveryUri">Uri of an OpenId connect endpoint for discovering the configuration</param>
        /// <param name="tokenValidationParameters">Collection of parameters to influence how the token validation is done</param>
        /// <param name="claimCheck">Custom claims key-value pair to validate against</param>
        /// <param name="logger">The logger instance to write diagnostic messages when verifying the JWT token.</param>/exception>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="tokenValidationParameters"/> or <paramref name="claimCheck"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="openIdConnectDiscoveryUri"/> is blank
        ///     or the <paramref name="claimCheck"/> doesn't have any entries or one of the entries has blank key/value inputs.
        /// </exception>
        public JwtTokenReader(
            TokenValidationParameters tokenValidationParameters, 
            string openIdConnectDiscoveryUri, 
            IDictionary<string, string> claimCheck, 
            ILogger<JwtTokenReader> logger)
        {
            Guard.NotNullOrWhitespace(openIdConnectDiscoveryUri, nameof(openIdConnectDiscoveryUri), "Requires an non-blank OpenId URI connection endpoint for discovering the OpenId configuration");
            Guard.NotNull(tokenValidationParameters, nameof(tokenValidationParameters), "Requires a collection of parameters to influence how the token validation is done");
            Guard.NotNull(claimCheck, nameof(claimCheck), "Requires a set of claim checks to verify the claims request JWT");
            Guard.NotAny(claimCheck, nameof(claimCheck), "Requires at least one entry in the set of claim checks to verify the claims in the request JWT");
            Guard.For<ArgumentException>(() => claimCheck.Any(item => String.IsNullOrWhiteSpace(item.Key) || String.IsNullOrWhiteSpace(item.Value)), 
                "Requires all entries in the set of claim checks to be non-blank to correctly verify the claims in the request JWT");

            _tokenValidationParameters = tokenValidationParameters;
            _claimCheck = claimCheck;
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
                TokenValidationParameters validationParameters = await DetermineTokenValidationParametersAsync();

                _logger.LogTrace(
                    "Verifying request JWT (ValidateAudience={ValidateAudience}, ValidateIssuer={ValidateIssuer}, ValidateIssuerSigningKey={ValidateIssuerSigningKey}, ValidateLifetime={ValidateLifetime}, ValidateTokenReplay={ValidateTokenReplay}, ValidateActor={ValidateActor})...",
                    validationParameters.ValidateAudience, validationParameters.ValidateIssuer, validationParameters.ValidateIssuerSigningKey, validationParameters.ValidateLifetime, validationParameters.ValidateTokenReplay, validationParameters.ValidateActor);
                
                _handler.ValidateToken(token, validationParameters, out SecurityToken jwtToken);
                _logger.LogTrace("Request JWT is considered valid!");

                bool result = ValidateClaimCheck(jwtToken);
                return result;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Unexpected failure during verifying the JWT");
                return false;
            }
        }

        /// <summary>
        /// Verifies if the claims coming in ClaimCheck object vs the claims in the jwt object based on the claim name and value are valid
        /// </summary>
        /// <param name="validatedToken">Security token which contains the jwt claims</param>
        /// <returns></returns>
        public bool ValidateClaimCheck(SecurityToken validatedToken)
        {
            if (_claimCheck is null || !_claimCheck.Any())
            {
                return true;
            }

            try
            {
                _logger.LogTrace("Verifying JWT claim check...");
                var jwtToken = (JwtSecurityToken) validatedToken;

                int validClaims = (from c in jwtToken.Claims
                    join m in _claimCheck on c.Type equals m.Key
                    where c.Value == m.Value
                    select c).Distinct().Count();

                bool result = _claimCheck.Count == validClaims;
                _logger.LogTrace("JWT claim check was considered {Result}", result ? "successful" : $"faulted, expected {_claimCheck.Count} but got {validClaims} valid claims");

                return result;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Unexpected failure occurred during verifying JWT claim check");
                return false;
            }
        }

        private async Task<TokenValidationParameters> DetermineTokenValidationParametersAsync()
        {
            if (_tokenValidationParameters is null)
            {
                OpenIdConnectConfiguration config = await _configManager.GetConfigurationAsync();

                var validationParameters = new TokenValidationParameters
                {
                    ValidateAudience = _claimCheck.ContainsKey(JwtClaimTypes.Audience),
                    ValidAudience = _claimCheck.ContainsKey(JwtClaimTypes.Audience) ? _claimCheck[JwtClaimTypes.Audience] : null,
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