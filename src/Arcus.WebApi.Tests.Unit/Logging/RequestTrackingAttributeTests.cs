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
        
        [Theory]
        [InlineData(HttpStatusCode.Created | HttpStatusCode.AlreadyReported)]
        [InlineData((HttpStatusCode) 600)]
        public void CreateAttribute_WithHttpStatusCodeOutOfExpectedRange_Fails(HttpStatusCode statusCode)
        {
            Assert.ThrowsAny<ArgumentException>(() => new RequestTrackingAttribute(statusCode));
        }
    }
}
