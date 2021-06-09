using System;
using Arcus.WebApi.Security.Authentication.SharedAccessKey;
using Xunit;

namespace Arcus.WebApi.Tests.Unit.Security.Authentication
{
    public class SharedAccessKeyAuthenticationFilterTests
    {
        [Theory]
        [InlineData(null, null, "not empty or whitespace")]
        [InlineData("", "", "not empty or whitespace")]
        [InlineData(" ", " ", "not empty or whitespace")]
        [InlineData("not empty or whitespace", "not empty or whitespace", null)]
        [InlineData("not empty or whitespace", "not empty or whitespace", "")]
        [InlineData("not empty or whitespace", "not empty or whitespace", " ")]
        public void SharedAccessKeyFilter_WithNotPresentHeaderNameQueryParameterNameAndOrSecretName_ShouldFailWithArgumentException(
            string headerName,
            string queryParameterName,
            string secretName)
        {
            Assert.Throws<ArgumentException>(
                () => new SharedAccessKeyAuthenticationFilter(headerName: headerName, queryParameterName: queryParameterName, secretName: secretName));
        }
    }
}
