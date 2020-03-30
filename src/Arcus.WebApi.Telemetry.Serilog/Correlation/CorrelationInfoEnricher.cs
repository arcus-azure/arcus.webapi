using System;
using Arcus.Observability.Correlation;
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
        
        private readonly IServiceProvider _services;

        /// <summary>
        /// Initializes a new instance of the <see cref="CorrelationInfoEnricher"/> class.
        /// </summary>
        public CorrelationInfoEnricher(IServiceProvider services)
        {
            Guard.NotNull(services, nameof(services));

            _services = services;
        }

        /// <summary>Enrich the log event.</summary>
        /// <param name="logEvent">The log event to enrich.</param>
        /// <param name="propertyFactory">Factory for creating new properties to add to the event.</param>
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            Guard.NotNull(logEvent, nameof(logEvent));
            Guard.NotNull(propertyFactory, nameof(propertyFactory));

            var correlationInfoAccessor = _services.GetRequiredService<ICorrelationInfoAccessor>();
            CorrelationInfo correlationInfo = correlationInfoAccessor.GetCorrelationInfo();

            if (!String.IsNullOrEmpty(correlationInfo.OperationId))
            {
                LogEventProperty property = propertyFactory.CreateProperty(OperationIdProperty, correlationInfo.OperationId);
                logEvent.AddPropertyIfAbsent(property);
            }

            if (!String.IsNullOrEmpty(correlationInfo.TransactionId))
            {
                LogEventProperty property = propertyFactory.CreateProperty(TransactionIdProperty, correlationInfo.TransactionId);
                logEvent.AddPropertyIfAbsent(property);
            }
        }
    }
}
