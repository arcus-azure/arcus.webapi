using System;
using Arcus.WebApi.Security.Authentication.Certificates;
using Xunit;

namespace Arcus.WebApi.Tests.Unit.Security.Authentication
{
    public class CertificateAuthenticationConfigBuilderTests
    {
        [Fact]
        public void Build_WithoutAnything_Fails()
        {
            // Arrange
            var builder = new CertificateAuthenticationConfigBuilder();

            // Act / Assert
            Assert.Throws<InvalidOperationException>(() => builder.Build());
        }
    }
}
