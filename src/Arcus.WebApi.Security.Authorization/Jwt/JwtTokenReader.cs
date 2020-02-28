using System;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using GuardNet;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Arcus.WebApi.Security.Authorization.Jwt
{
    public class JwtTokenReader : IJwtTokenReader
    {
        private const string MicrosoftDiscoveryEndpoint =
            "https://login.microsoftonline.com/common/v2.0/.well-known/openid-configuration";

        private readonly string _applicationId;
        private readonly JwtSecurityTokenHandler _handler;
        private readonly ConfigurationManager<OpenIdConnectConfiguration> _configManager;

        public JwtTokenReader(string applicationId)
        {
            Guard.NotNullOrWhitespace(applicationId, nameof(applicationId));

            _applicationId = applicationId;

            _handler = new JwtSecurityTokenHandler();
            _configManager = new ConfigurationManager<OpenIdConnectConfiguration>(MicrosoftDiscoveryEndpoint,
                new OpenIdConnectConfigurationRetriever());
        }

        /// <summary>
        ///     Validates if the token is considered valid
        /// </summary>
        /// <param name="token">JWT token</param>
        public async Task<bool> IsValidToken(string token)
        {
            OpenIdConnectConfiguration config = await _configManager.GetConfigurationAsync();

            var validationParameters = new TokenValidationParameters
            {
                ValidateAudience = true,
                ValidAudience = _applicationId,
                ValidateIssuer = false,
                IssuerSigningKeys = config.SigningKeys,
                ValidateLifetime = true
            };

            SecurityToken jwtToken;
            try
            {
                _handler.ValidateToken(token, validationParameters, out jwtToken);
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }
    }
}