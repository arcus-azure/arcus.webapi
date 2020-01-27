using System;
using Arcus.WebApi.Correlation;
using GuardNet;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Core;
using Serilog.Events;

namespace Arcus.WebApi.Telemetry.Serilog.Correlation
{
    /// <summary>
    /// Enriches the log events with the correlation information.
    /// </summary>
    public class CorrelationInfoEnricher : ILogEventEnricher
    {
        private const string TransactionIdProperty = "TransactionId",
                             OperationIdProperty = "OperationId";

        private readonly HttpCorrelationInfo _correlationInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="CorrelationInfoEnricher"/> class.
        /// </summary>
        /// <param name="correlationInfo">The correlation information registered in the <see cref="IServiceCollection"/>.</param>
        public CorrelationInfoEnricher(HttpCorrelationInfo correlationInfo)
        {
            Guard.NotNull(correlationInfo, nameof(correlationInfo));

            _correlationInfo = correlationInfo;
        }

        /// <summary>
        /// Enrich the log event.
        /// </summary>
        /// <param name="logEvent">The log event to enrich.</param>
        /// <param name="propertyFactory">Factory for creating new properties to add to the event.</param>
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            if (!String.IsNullOrEmpty(_correlationInfo.TransactionId))
            {
                LogEventProperty transactionIdProperty = 
                    propertyFactory.CreateProperty(TransactionIdProperty, _correlationInfo.TransactionId);

                logEvent.AddPropertyIfAbsent(transactionIdProperty);
            }

            if (!String.IsNullOrEmpty(_correlationInfo.OperationId))
            {
                LogEventProperty operationIdProperty =
                    propertyFactory.CreateProperty(OperationIdProperty, _correlationInfo.OperationId);

                logEvent.AddPropertyIfAbsent(operationIdProperty);
            }
        }
    }
}
