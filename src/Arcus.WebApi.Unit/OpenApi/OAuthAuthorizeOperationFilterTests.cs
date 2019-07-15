using Arcus.WebApi.OpenApi.Extensions;
using System;
using System.Collections.Generic;
using Xunit;

namespace Arcus.WebApi.Unit.OpenApi
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
    }
}
