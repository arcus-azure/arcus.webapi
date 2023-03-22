using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Observability.Correlation;
using Arcus.Observability.Telemetry.Core;
using GuardNet;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Arcus.WebApi.Logging.Core.Correlation
{
    /// <summary>
    /// Represents an HTTP message handler implementation (<see cref="DelegatingHandler"/>) that enriches the HTTP request with correlation information.
    /// </summary>
    public class HttpCorrelationMessageHandler : DelegatingHandler
    {
        private readonly IHttpCorrelationInfoAccessor _correlationInfoAccessor;
        private readonly HttpCorrelationClientOptions _options;
        private readonly ILogger<HttpCorrelationMessageHandler> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpCorrelationMessageHandler" /> class.
        /// </summary>
        /// <param name="correlationInfoAccessor">The accessor of the current HTTP context.</param>
        /// <param name="options">The additional set of options to influence the HTTP dependency tracking.</param>
        /// <param name="logger">The logger instance to write the HTTP dependency telemetry.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="correlationInfoAccessor"/>, <paramref name="options"/>, or <paramref name="logger"/> is <c>null</c>.
        /// </exception>
        public HttpCorrelationMessageHandler(
            IHttpCorrelationInfoAccessor correlationInfoAccessor, 
            HttpCorrelationClientOptions options, 
            ILogger<HttpCorrelationMessageHandler> logger)
        {
            Guard.NotNull(correlationInfoAccessor, nameof(correlationInfoAccessor), "Requires a HTTP context accessor to retrieve the current HTTP correlation");
            Guard.NotNull(options, nameof(options), "Requires a set of additional user-configurable options to influence the HTTP dependency tracking");
            Guard.NotNull(logger, nameof(logger), "Requires a logger instance to write the HTTP dependency telemetry");

            _correlationInfoAccessor = correlationInfoAccessor;
            _options = options;
            _logger = logger;
        }

        /// <summary>
        /// Sends an HTTP request to the inner handler to send to the server as an asynchronous operation.
        /// </summary>
        /// <param name="request">The HTTP request message to send to the server.</param>
        /// <param name="cancellationToken">A cancellation token to cancel operation.</param>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="request" /> was <see langword="null" />.</exception>
        /// <returns>The task object representing the asynchronous operation.</returns>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var statusCode = default(HttpStatusCode);
            string dependencyId = _options.GenerateDependencyId();
            request.Headers.Add(_options.UpstreamServiceHeaderName, dependencyId);

            CorrelationInfo correlation = DetermineCorrelationInfo();
            request.Headers.Add(_options.TransactionIdHeaderName, correlation.TransactionId);
            
            using (var measurement = DurationMeasurement.Start())
            {
                try
                {
                    HttpResponseMessage response = await base.SendAsync(request, cancellationToken);
                    statusCode = response.StatusCode;

                    return response;
                }
                finally
                {
                    _logger.LogHttpDependency(request, statusCode, measurement, dependencyId, _options.TelemetryContext);
                }
            }
        }

        private CorrelationInfo DetermineCorrelationInfo()
        {
            CorrelationInfo correlation = _correlationInfoAccessor.GetCorrelationInfo();
            if (correlation is null)
            {
                throw new InvalidOperationException(
                    "Cannot enrich the HTTP request with HTTP correlation because no HTTP correlation was registered in the application, " 
                    + "make sure that you register the HTTP correlation services with 'services.AddHttpCorrelation()' " 
                    + "and that you use the HTTP correlation middleware 'app.UseHttpCorrelation()' in API scenario's");
            }

            return correlation;
        }
    }
}
