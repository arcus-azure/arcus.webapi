using System;
using Arcus.WebApi.OpenApi.Extensions;
using Xunit;
#if NETCOREAPP3_1
using Microsoft.OpenApi.Models;
#endif

namespace Arcus.WebApi.Tests.Unit.OpenApi
{
    public class SharedAccessKeyAuthenticationOperationFilterTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        public void OperationFilter_WithoutSecuritySchemaName_Throws(string securitySchemaName)
        {
            Assert.ThrowsAny<ArgumentException>(
                () => new SharedAccessKeyAuthenticationOperationFilter(securitySchemaName));
        }

#if NETCOREAPP3_1
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        public void OperationFilterNetCoreApp31_WithoutSecuritySchemeName_Throws(string securitySchemeName)
        {
            Assert.ThrowsAny<ArgumentException>(
                () => new SharedAccessKeyAuthenticationOperationFilter(securitySchemeName: securitySchemeName));
        }

        [Fact]
        public void OperationFilterNetCore31_WithOutOfBoundsSecuritySchemeType_Throws()
        {
            Assert.ThrowsAny<ArgumentException>(
                () => new SharedAccessKeyAuthenticationOperationFilter(securitySchemeType: (SecuritySchemeType) 12));
        }
#endif
    }
}
