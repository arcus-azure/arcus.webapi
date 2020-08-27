using System;
using System.Collections.Generic;
using System.Linq;
using Arcus.WebApi.OpenApi.Extensions;
using Xunit;

namespace Arcus.WebApi.Tests.Unit.OpenApi
{
    public class OAuthAuthorizeOperationFilterTests
    {
        [Theory]
        [InlineData(new object[] { new[] { "valid scope", "" } })]
        [InlineData(new object[] { new[] { "valid scope", null, "another scope" } })]
        public void OAuthAuthorizeOperationFilter_ShouldFailWithInvalidScopeList(IEnumerable<string> scopes)
        {
            Assert.Throws<ArgumentException>(() => new OAuthAuthorizeOperationFilter(scopes));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        public void OAuthAuthorizeOperationFilter_WithoutSecuritySchemaName_Throws(string securitySchemaName)
        {
            Assert.ThrowsAny<ArgumentException>(
                () => new OAuthAuthorizeOperationFilter(Enumerable.Empty<string>(), securitySchemaName));
        }
    }
}
