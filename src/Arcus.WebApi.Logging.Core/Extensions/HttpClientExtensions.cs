using System.Threading.Tasks;
using Arcus.Observability.Correlation;
using Arcus.Observability.Telemetry.Core;
using Arcus.WebApi.Logging.Core.Correlation;
using GuardNet;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace System.Net.Http
{
    /// <summary>
    /// Extensions on the <see cref="HttpClient"/> to track HTTP correlation while sending HTTP requests.
    /// </summary>
    public static class HttpClientExtensions
    {
        /// <summary>
        /// Sends an HTTP request as an asynchronous operation while tracking the HTTP correlation.
        /// </summary>
        /// <remarks>
        ///     Note that when you use the W3C correlation system, you don't need to explicitly add HTTP correlation tracking because Microsoft tracks dependencies automatically for you.
        ///     This way of sending correlated HTTP requests is not needed if you used
        ///     <see cref="IHttpClientBuilderExtensions.WithHttpCorrelationTracking(IHttpClientBuilder)"/> to register <see cref="HttpClient"/> instances.
        ///     This extension is only needed when the <see cref="HttpClient"/> used here is created by yourself,
        ///     otherwise use the regular <see cref="HttpClient.SendAsync(HttpRequestMessage)"/> to send the HTTP request and the request will be correlated automatically.
        /// </remarks>
        /// <param name="client">The client to send the <paramref name="request"/>.</param>
        /// <param name="request">The HTTP request message to send.</param>
        /// <param name="correlationAccessor">The HTTP correlation accessor to retrieve the current correlation available to track with the <paramref name="request"/>.</param>
        /// <param name="logger">The logger instance to write the HTTP dependency while tracking the <paramref name="request"/>.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="client"/>, <paramref name="request"/>, <paramref name="correlationAccessor"/>, <paramref name="logger"/> is <c>null</c>.
        /// </exception>
        public static async Task<HttpResponseMessage> SendAsync(
            this HttpClient client,
            HttpRequestMessage request,
            IHttpCorrelationInfoAccessor correlationAccessor,
            ILogger logger)
        {
            Guard.NotNull(client, nameof(client), "Requires a HTTP client to track the HTTP request with HTTP correlation");
            Guard.NotNull(request, nameof(request), "Requires a HTTP request to enrich with HTTP correlation");
            Guard.NotNull(correlationAccessor, nameof(correlationAccessor), "Requires a HTTP correlation accessor instance to retrieve the current correlation to include in the HTTP request");
            Guard.NotNull(logger, nameof(logger), "Requires a logger instance to track the correlated HTTP request");

            return await SendAsync(client, request, correlationAccessor, logger, configureOptions: null);
        }

        /// <summary>
        /// Sends an HTTP request as an asynchronous operation while tracking the HTTP correlation.
        /// </summary>
        /// <remarks>
        ///     Note that when you use the W3C correlation system, you don't need to explicitly add HTTP correlation tracking because Microsoft tracks dependencies automatically for you.
        ///     This way of sending correlated HTTP requests is not needed if you used
        ///     <see cref="IHttpClientBuilderExtensions.WithHttpCorrelationTracking(IHttpClientBuilder)"/> to register <see cref="HttpClient"/> instances.
        ///     This extension is only needed when the <see cref="HttpClient"/> used here is created by yourself,
        ///     otherwise use the regular <see cref="HttpClient.SendAsync(HttpRequestMessage)"/> to send the HTTP request and the request will be correlated automatically.
        /// </remarks>
        /// <param name="client">The client to send the <paramref name="request"/>.</param>
        /// <param name="request">The HTTP request message to send.</param>
        /// <param name="correlationAccessor">The HTTP correlation accessor to retrieve the current correlation available to track with the <paramref name="request"/>.</param>
        /// <param name="logger">The logger instance to write the HTTP dependency while tracking the <paramref name="request"/>.</param>
        /// <param name="configureOptions">The additional options to configure how the <paramref name="request"/> must be tracked.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="client"/>, <paramref name="request"/>, <paramref name="correlationAccessor"/>, <paramref name="logger"/> is <c>null</c>.
        /// </exception>
        public static async Task<HttpResponseMessage> SendAsync(
            this HttpClient client,
            HttpRequestMessage request,
            IHttpCorrelationInfoAccessor correlationAccessor,
            ILogger logger,
            Action<HttpCorrelationClientOptions> configureOptions)
        {
            Guard.NotNull(client, nameof(client), "Requires a HTTP client to track the HTTP request with HTTP correlation");
            Guard.NotNull(request, nameof(request), "Requires a HTTP request to enrich with HTTP correlation");
            Guard.NotNull(correlationAccessor, nameof(correlationAccessor), "Requires a HTTP correlation accessor instance to retrieve the current correlation to include in the HTTP request");
            Guard.NotNull(logger, nameof(logger), "Requires a logger instance to track the correlated HTTP request");

            CorrelationInfo correlation = correlationAccessor.GetCorrelationInfo();
            return await SendAsync(client, request, correlation, logger, configureOptions);
        }

        /// <summary>
        /// Sends an HTTP request as an asynchronous operation while tracking the HTTP correlation.
        /// </summary>
        /// <remarks>
        ///     Note that when you use the W3C correlation system, you don't need to explicitly add HTTP correlation tracking because Microsoft tracks dependencies automatically for you.
        ///     This way of sending correlated HTTP requests is not needed if you used
        ///     <see cref="IHttpClientBuilderExtensions.WithHttpCorrelationTracking(IHttpClientBuilder)"/> to register <see cref="HttpClient"/> instances.
        ///     This extension is only needed when the <see cref="HttpClient"/> used here is created by yourself,
        ///     otherwise use the regular <see cref="HttpClient.SendAsync(HttpRequestMessage)"/> to send the HTTP request and the request will be correlated automatically.
        /// </remarks>
        /// <param name="client">The client to send the <paramref name="request"/>.</param>
        /// <param name="request">The HTTP request message to send.</param>
        /// <param name="correlationInfo">The current HTTP correlation available to track with the <paramref name="request"/>.</param>
        /// <param name="logger">The logger instance to write the HTTP dependency while tracking the <paramref name="request"/>.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="client"/>, <paramref name="request"/>, <paramref name="correlationInfo"/>, <paramref name="logger"/> is <c>null</c>.
        /// </exception>
        public static async Task<HttpResponseMessage> SendAsync(
            this HttpClient client,
            HttpRequestMessage request,
            CorrelationInfo correlationInfo,
            ILogger logger)
        {
            Guard.NotNull(client, nameof(client), "Requires a HTTP client to track the HTTP request with HTTP correlation");
            Guard.NotNull(request, nameof(request), "Requires a HTTP request to enrich with HTTP correlation");
            Guard.NotNull(correlationInfo, nameof(correlationInfo), "Requires a HTTP correlation instance to include in the HTTP request");
            Guard.NotNull(logger, nameof(logger), "Requires a logger instance to track the correlated HTTP request");

            return await SendAsync(client, request, correlationInfo, logger, configureOptions: null);
        }

        /// <summary>
        /// Sends an HTTP request as an asynchronous operation while tracking the HTTP correlation.
        /// </summary>
        /// <remarks>
        ///     Note that when you use the W3C correlation system, you don't need to explicitly add HTTP correlation tracking because Microsoft tracks dependencies automatically for you.
        ///     This way of sending correlated HTTP requests is not needed if you used
        ///     <see cref="IHttpClientBuilderExtensions.WithHttpCorrelationTracking(IHttpClientBuilder)"/> to register <see cref="HttpClient"/> instances.
        ///     This extension is only needed when the <see cref="HttpClient"/> used here is created by yourself,
        ///     otherwise use the regular <see cref="HttpClient.SendAsync(HttpRequestMessage)"/> to send the HTTP request and the request will be correlated automatically.
        /// </remarks>
        /// <param name="client">The client to send the <paramref name="request"/>.</param>
        /// <param name="request">The HTTP request message to send.</param>
        /// <param name="correlationInfo">The current HTTP correlation available to track with the <paramref name="request"/>.</param>
        /// <param name="logger">The logger instance to write the HTTP dependency while tracking the <paramref name="request"/>.</param>
        /// <param name="configureOptions">The additional options to configure how the <paramref name="request"/> must be tracked.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the <paramref name="client"/>, <paramref name="request"/>, <paramref name="correlationInfo"/>, <paramref name="logger"/> is <c>null</c>.
        /// </exception>
        public static async Task<HttpResponseMessage> SendAsync(
            this HttpClient client, 
            HttpRequestMessage request, 
            CorrelationInfo correlationInfo, 
            ILogger logger,
            Action<HttpCorrelationClientOptions> configureOptions)
        {
            Guard.NotNull(client, nameof(client), "Requires a HTTP client to track the HTTP request with HTTP correlation");
            Guard.NotNull(request, nameof(request), "Requires a HTTP request to enrich with HTTP correlation");
            Guard.NotNull(correlationInfo, nameof(correlationInfo), "Requires a HTTP correlation instance to include in the HTTP request");
            Guard.NotNull(logger, nameof(logger), "Requires a logger instance to track the correlated HTTP request");

            var options = new HttpCorrelationClientOptions();
            configureOptions?.Invoke(options);
            string dependencyId = options.GenerateDependencyId();
            var statusCode = default(HttpStatusCode);

            request.Headers.Add(options.UpstreamServiceHeaderName, dependencyId);
            request.Headers.Add(options.TransactionIdHeaderName, correlationInfo.TransactionId);

            using (var measurement = DurationMeasurement.Start())
            {
                try
                {
                    HttpResponseMessage response = await client.SendAsync(request);
                    statusCode = response.StatusCode;

                    return response;
                }
                finally
                {
                    logger.LogHttpDependency(request, statusCode, measurement, dependencyId, options.TelemetryContext);
                } 
            }
        }
    }
}
