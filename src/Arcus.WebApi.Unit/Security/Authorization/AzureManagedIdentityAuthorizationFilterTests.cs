using System;
using Arcus.WebApi.Security.Authorization;
using Arcus.WebApi.Security.Authorization.Jwt;
using Xunit;

namespace Arcus.WebApi.Unit.Security.Authorization
{
    public class AzureManagedIdentityAuthorizationFilterTests
    {
        [Fact]
        public void Constructor_WithoutJwtTokenReader_ThrowsException()
        {
            // Arrange
            JwtTokenReader jwtTokenReader = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new AzureManagedIdentityAuthorizationFilter(jwtTokenReader));
        }

        [Fact]
        public void Constructor_WithoutHttpHeader_ThrowsException()
        {
            // Arrange
            string headerName = null;
            string applicationId = Guid.NewGuid().ToString();
            JwtTokenReader jwtTokenReader = new JwtTokenReader(applicationId);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new AzureManagedIdentityAuthorizationFilter(headerName, jwtTokenReader));
        }

        [Fact]
        public void Constructor_WithHttpHeaderButWithoutJwtTokenReader_ThrowsException()
        {
            // Arrange
            const string headerName = "x-app-token";
            JwtTokenReader jwtTokenReader = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new AzureManagedIdentityAuthorizationFilter(headerName, jwtTokenReader));
        }

        [Fact]
        public void Constructor_Valid_Succeeds()
        {
            // Arrange
            const string headerName = "x-app-token";
            string applicationId = Guid.NewGuid().ToString();
            JwtTokenReader jwtTokenReader = new JwtTokenReader(applicationId);

            // Act
            var azureManagedIdentityAuthorizationFilter = new AzureManagedIdentityAuthorizationFilter(headerName, jwtTokenReader);

            // Assert
            Assert.NotNull(azureManagedIdentityAuthorizationFilter);
        }
    }
}