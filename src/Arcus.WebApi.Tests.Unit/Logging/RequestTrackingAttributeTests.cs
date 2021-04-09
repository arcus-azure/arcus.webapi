using System;
using System.Net;
using Arcus.WebApi.Logging;
using Xunit;

namespace Arcus.WebApi.Tests.Unit.Logging
{
    public class RequestTrackingAttributeTests
    {
        [Theory]
        [InlineData(Exclude.None)]
        [InlineData(Exclude.RequestBody | Exclude.ResponseBody)]
        [InlineData((Exclude) 5)]
        public void CreateAttribute_WithExclusionOutOfExpectedRange_Fails(Exclude filter)
        {
            Assert.ThrowsAny<ArgumentException>(() => new RequestTrackingAttribute(filter));
        }

        [Fact]
        public void CreateAttribute_WithNullResponseStatusCodes_Fails()
        {
            Assert.ThrowsAny<ArgumentException>(() => new RequestTrackingAttribute(trackedStatusCodes: null));
        }

        [Fact]
        public void CreateAttribute_WithoutResponseStatusCodes_Fails()
        {
            Assert.ThrowsAny<ArgumentException>(() => new RequestTrackingAttribute());
        }
        
        [Fact]
        public void CreateAttribute_WithZeroResponseStatusCodes_Fails()
        {
            Assert.ThrowsAny<ArgumentException>(() => new RequestTrackingAttribute(Array.Empty<HttpStatusCode>()));
        }
    }
}
