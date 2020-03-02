using System;
using Arcus.WebApi.Security.Authorization.Jwt;
using Xunit;

namespace Arcus.WebApi.Unit.Security.Authorization
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
    }
}