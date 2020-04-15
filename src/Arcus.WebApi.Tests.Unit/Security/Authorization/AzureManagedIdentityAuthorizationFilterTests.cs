using System;
using Arcus.WebApi.Security.Authorization;
using Arcus.WebApi.Security.Authorization.Jwt;
using Xunit;

namespace Arcus.WebApi.Tests.Unit.Security.Authorization
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
    }
}