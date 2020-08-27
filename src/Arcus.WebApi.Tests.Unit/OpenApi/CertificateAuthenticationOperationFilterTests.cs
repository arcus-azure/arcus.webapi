using System;
using Arcus.WebApi.OpenApi.Extensions;
using Xunit;
#if NETCOREAPP3_1
using Microsoft.OpenApi.Models;
#endif

namespace Arcus.WebApi.Tests.Unit.OpenApi
{
    public class CertificateAuthenticationOperationFilterTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        public void OperationFilter_WithoutSecuritySchemaName_Throws(string securitySchemaName)
        {
            Assert.ThrowsAny<ArgumentException>(() => new CertificateAuthenticationOperationFilter(securitySchemaName));
        }

#if NETCOREAPP3_1
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        public void OperationFilterNetCoreApp31_WithoutSecuritySchemeName_Throws(string securitySchemeName)
        {
            Assert.ThrowsAny<ArgumentException>(
                () => new CertificateAuthenticationOperationFilter(securitySchemeName: securitySchemeName));
        }

        [Theory]
        [InlineData(SecuritySchemeType.Http | SecuritySchemeType.OAuth2)]
        [InlineData((SecuritySchemeType) 4)]
        public void OperationFilterNetCore31_WithOutOfBoundsSecuritySchemeType_Throws(SecuritySchemeType securitySchemeType)
        {
            Assert.ThrowsAny<ArgumentException>(
                () => new CertificateAuthenticationOperationFilter(securitySchemeType: securitySchemeType));
        }
#endif
    }
}
