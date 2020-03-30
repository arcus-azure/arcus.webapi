using System;
using Arcus.WebApi.Security.Authorization.Jwt;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Arcus.WebApi.Tests.Unit.Security.Authorization
{
    public class JwtTokenReaderTests
    {
        [Fact]
        public void Constructor_WithoutApplicationId_ThrowsException()
        {
            // Arrange
            string applicationId = null;

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new JwtTokenReader(applicationId));
        }

        [Fact]
        public void Constructor_WithApplicationId_Succeeds()
        {
            // Arrange
            string applicationId = Guid.NewGuid().ToString();

            // Act
            var jwtTokenReader = new JwtTokenReader(applicationId);

            // Assert
            Assert.NotNull(jwtTokenReader);
        }

        [Fact]
        public void Constructor_WithTokenValidationParameters_Succeeds()
        {
            // Arrange
            var tokenValidationParameters = new TokenValidationParameters();

            // Act
            var jwtTokenReader = new JwtTokenReader(tokenValidationParameters);

            // Assert
            Assert.NotNull(jwtTokenReader);
        }

        [Fact]
        public void Constructor_WithTokenValidationParametersButWithoutDiscoveryEndpoint_ThrowsException()
        {
            // Arrange
            var tokenValidationParameters = new TokenValidationParameters();
            string openIdConnectDiscoveryUri = string.Empty;

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new JwtTokenReader(tokenValidationParameters, openIdConnectDiscoveryUri));
        }

        [Fact]
        public void Constructor_WithoutTokenValidationParameters_Succeeds()
        {
            // Arrange
            TokenValidationParameters tokenValidationParameters = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new JwtTokenReader(tokenValidationParameters));
        }

        [Fact]
        public void Constructor_WithTokenValidationParametersAndDiscoveryEndpoint_Succeeds()
        {
            // Arrange
            var tokenValidationParameters = new TokenValidationParameters();
            string openIdConnectDiscoveryUri = "https://login.microsoftonline.com/common/v2.0/.well-known/openid-configuration";

            // Act
            var jwtTokenReader = new JwtTokenReader(tokenValidationParameters, openIdConnectDiscoveryUri);

            // Assert
            Assert.NotNull(jwtTokenReader);
        }
    }
}