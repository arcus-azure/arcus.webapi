using System;
using Arcus.WebApi.Security.Authentication.Certificates;
using Xunit;

namespace Arcus.WebApi.Tests.Unit.Security.Authentication
{
    public class CertificateAuthenticationFilterTests
    {
        [Fact]
        public void Create_WithoutCertificateAuthenticationOptions_Fails()
        {
            Assert.ThrowsAny<ArgumentException>(
                () => new CertificateAuthenticationFilter(options: null));
        }
    }
}
