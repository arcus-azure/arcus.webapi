using System;
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
    }
}
