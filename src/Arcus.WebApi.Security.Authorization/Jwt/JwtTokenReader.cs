﻿using System;
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
        private readonly TokenValidationParameters _tokenValidationParameters;
        private readonly JwtSecurityTokenHandler _handler = new JwtSecurityTokenHandler();
        private readonly ConfigurationManager<OpenIdConnectConfiguration> _configManager;

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <remarks>Uses Microsoft OpenId connect discovery endpoint</remarks>
        /// <param name="applicationId">Azure AD Application used as audience to validate against</param>
        public JwtTokenReader(string applicationId)
        {
            Guard.NotNullOrWhitespace(applicationId, nameof(applicationId));

            _applicationId = applicationId;
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
        ///     Validates if the token is considered valid
        /// </summary>
        /// <param name="token">JWT token</param>
        public async Task<bool> IsValidTokenAsync(string token)
        {
            OpenIdConnectConfiguration config = await _configManager.GetConfigurationAsync();

            var validationParameters = _tokenValidationParameters ?? new TokenValidationParameters
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