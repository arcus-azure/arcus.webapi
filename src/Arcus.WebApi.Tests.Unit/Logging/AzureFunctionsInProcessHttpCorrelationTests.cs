using System;
using Arcus.Observability.Correlation;
using Arcus.WebApi.Logging.AzureFunctions.Correlation;
using Arcus.WebApi.Logging.Core.Correlation;
using Arcus.WebApi.Tests.Unit.Logging.Fixture;
using Bogus;
using Bogus.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Arcus.WebApi.Tests.Unit.Logging
{
    public class AzureFunctionsInProcessHttpCorrelationTests
    {
        private static readonly Faker BogusGenerator = new Faker();

        [Fact]
        public void GetCorrelationInfo_WithAccessorResult_Succeeds()
        {
            // Arrange
            var expected = new CorrelationInfo($"operation-{Guid.NewGuid()}", $"transaction-{Guid.NewGuid()}", $"parent-{Guid.NewGuid()}")
                .OrNull(BogusGenerator);
            
            var service = new AzureFunctionsInProcessHttpCorrelation(
                new HttpCorrelationInfoOptions(), 
                new StubHttpCorrelationInfoAccessor(expected), 
                NullLogger<AzureFunctionsInProcessHttpCorrelation>.Instance);

            // Act
            CorrelationInfo actual = service.GetCorrelationInfo();

            // Assert
            Assert.Same(expected, actual);
        }
    }
}
