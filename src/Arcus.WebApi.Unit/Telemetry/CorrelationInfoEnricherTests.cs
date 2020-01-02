using System;
using System.Collections.Generic;
using Arcus.WebApi.Correlation;
using Arcus.WebApi.Telemetry.Serilog.Correlation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Moq;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Xunit;

namespace Arcus.WebApi.Unit.Telemetry
{
    public class CorrelationInfoEnricherTests 
    {
        [Fact]
        public void LogEvent_WithCorrelationInfoEnricher_HasCorrelationProperties()
        {
            // Arrange
            string operationId = $"operation-{Guid.NewGuid()}";
            string transactionId = $"transaction-{Guid.NewGuid()}";

           HttpCorrelationInfo httpCorrelationInfo = CreateHttpCorrelationInfo(operationId, transactionId);
            var logEventSinkSpy = new Mock<ILogEventSink>();

            ILogger logger = new LoggerConfiguration()
                .Enrich.WithCorrelation(httpCorrelationInfo)
                .WriteTo.Sink(logEventSinkSpy.Object)
                .CreateLogger();

            // Act
            logger.Information("Has correlation information as properties");

            // Assert
            logEventSinkSpy.Verify(
                spy => spy.Emit(It.Is<LogEvent>(
                    env => HasScalarProperty(env.Properties, nameof(CorrelationInfo.OperationId), operationId)
                           && HasScalarProperty(env.Properties, nameof(CorrelationInfo.TransactionId), transactionId))));
        }

        private static HttpCorrelationInfo CreateHttpCorrelationInfo(string operationId, string transactionId)
        {
            var features = new FeatureCollection
            {
                [typeof(CorrelationInfo)] = new CorrelationInfo(operationId, transactionId)
            };

            var httpContextStub = new Mock<HttpContext>();
            httpContextStub.Setup(ctx => ctx.Features).Returns(features);

            var httpContextAccessorStub = new Mock<IHttpContextAccessor>();
            httpContextAccessorStub.Setup(ctx => ctx.HttpContext).Returns(httpContextStub.Object);

            return new HttpCorrelationInfo(httpContextAccessorStub.Object);
        }

        private static bool HasScalarProperty(
            IReadOnlyDictionary<string, LogEventPropertyValue> properties,
            string key,
            string expected)
        {
            return ((ScalarValue) properties[key]).Value.ToString().Equals(expected);
        }
    }
}
