using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using GuardNet;
using IdentityModel;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Arcus.WebApi.Security.Authorization.Jwt
{
    /// <summary>
    /// <see cref="IJwtTokenReader"/> implementation to verify with OpenID configuration the JWT token from the HTTP request header.
    /// </summary>
    public class JwtTokenReader : IJwtTokenReader
    {
        private const string MicrosoftDiscoveryEndpoint =
            "https://login.microsoftonline.com/common/v2.0/.well-known/openid-configuration";

        private readonly IDictionary<string, string> _claimCheck;
        private readonly TokenValidationParameters _tokenValidationParameters;
        private readonly JwtSecurityTokenHandler _handler = new JwtSecurityTokenHandler();
        private readonly ConfigurationManager<OpenIdConnectConfiguration> _configManager;

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <remarks>Uses Microsoft OpenId connect discovery endpoint</remarks>
        /// <param name="claimCheck">Azure AD Application used as audience to validate against</param>
        public JwtTokenReader(IDictionary<string, string> claimCheck)
        {
            Guard.NotAny(claimCheck, nameof(claimCheck));

            _claimCheck = claimCheck;

            _configManager = new ConfigurationManager<OpenIdConnectConfiguration>(MicrosoftDiscoveryEndpoint, new OpenIdConnectConfigurationRetriever());
        }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <remarks>Uses Microsoft OpenId connect discovery endpoint</remarks>
        /// <param name="tokenValidationParameters">Collection of parameters to influence how the token validation is done</param>
        public JwtTokenReader(TokenValidationParameters tokenValidationParameters) : this(tokenValidationParameters, MicrosoftDiscoveryEndpoint)
        {
        }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <remarks>Uses Microsoft OpenId connect discovery endpoint</remarks>
        /// <param name="tokenValidationParameters">Collection of parameters to influence how the token validation is done</param>
        /// <param name="claimCheck">Custom claims key-value pair to validate against</param>
        public JwtTokenReader(TokenValidationParameters tokenValidationParameters, IDictionary<string, string> claimCheck) : this(tokenValidationParameters, MicrosoftDiscoveryEndpoint, claimCheck)
        {
        }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="applicationId">Azure AD Application used as audience to validate against</param>
        public JwtTokenReader(string applicationId) : this(new Dictionary<string, string> {{ JwtClaimTypes.Audience, applicationId}})
        {
            Guard.NotNullOrWhitespace(applicationId, nameof(applicationId));
        }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="openIdConnectDiscoveryUri">Uri of an OpenId connect endpoint for discovering the configuration</param>
        /// <param name="tokenValidationParameters">Collection of parameters to influence how the token validation is done</param>
        public JwtTokenReader(TokenValidationParameters tokenValidationParameters, string openIdConnectDiscoveryUri)
        {
            Guard.NotNullOrWhitespace(openIdConnectDiscoveryUri, nameof(openIdConnectDiscoveryUri));
            Guard.NotNull(tokenValidationParameters, nameof(tokenValidationParameters));

            _tokenValidationParameters = tokenValidationParameters;
            _configManager = new ConfigurationManager<OpenIdConnectConfiguration>(openIdConnectDiscoveryUri, new OpenIdConnectConfigurationRetriever());
        }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="openIdConnectDiscoveryUri">Uri of an OpenId connect endpoint for discovering the configuration</param>
        /// <param name="tokenValidationParameters">Collection of parameters to influence how the token validation is done</param>
        /// <param name="claimCheck">Custom claims key-value pair to validate against</param>
        public JwtTokenReader(TokenValidationParameters tokenValidationParameters, string openIdConnectDiscoveryUri, IDictionary<string, string> claimCheck)
        {
            Guard.NotNullOrWhitespace(openIdConnectDiscoveryUri, nameof(openIdConnectDiscoveryUri));
            Guard.NotNull(tokenValidationParameters, nameof(tokenValidationParameters));
            Guard.NotAny(claimCheck, nameof(claimCheck));

            _tokenValidationParameters = tokenValidationParameters;
            _claimCheck = claimCheck;
            _configManager = new ConfigurationManager<OpenIdConnectConfiguration>(openIdConnectDiscoveryUri, new OpenIdConnectConfigurationRetriever());
        }

        /// <summary>
        ///     Verify if the token is considered valid.
        /// </summary>
        /// <param name="token">The JWT token.</param>
        public async Task<bool> IsValidTokenAsync(string token)
        {
            bool result;

            try
            {
                TokenValidationParameters validationParameters = await DetermineTokenValidationParametersAsync();
                _handler.ValidateToken(token, validationParameters, out var validatedToken);
                result = ValidateClaimCheck(validatedToken);
            }
            catch
            {
                return false;
            }

            return result;
        }

        /// <summary>
        /// Verifies if the claims coming in ClaimCheck object vs the claims in the jwt object based on the claim name and value are valid
        /// </summary>
        /// <param name="validatedToken">Security token which contains the jwt claims</param>
        /// <returns></returns>
        public bool ValidateClaimCheck(SecurityToken validatedToken)
        {
            if (_claimCheck == null || !_claimCheck.Any())
            {
                return true;
            }

            try
            {
                JwtSecurityToken jwtToken = (JwtSecurityToken)validatedToken;

                int validClaims = (from c in jwtToken.Claims
                    join m in _claimCheck on c.Type equals m.Key
                    where c.Value == m.Value
                    select c).Distinct().Count();

                return _claimCheck.Count == validClaims;
            }
            catch
            {
                return false;
            }
        }

        private async Task<TokenValidationParameters> DetermineTokenValidationParametersAsync()
        {
            if (_tokenValidationParameters is null)
            {
                OpenIdConnectConfiguration config = await _configManager.GetConfigurationAsync();

                TokenValidationParameters validationParameters = new TokenValidationParameters
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