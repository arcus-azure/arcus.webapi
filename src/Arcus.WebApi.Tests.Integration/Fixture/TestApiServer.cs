using System;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.Testing.Logging;
using GuardNet;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Arcus.WebApi.Tests.Integration.Fixture
{
    /// <summary>
    /// Represents a test API server which can be used to mimic a real-life hosted web API application.
    /// </summary>
    public class TestApiServer : IAsyncDisposable
    {
        private readonly IHost _host;
        private readonly TestApiServerOptions _options;
        private readonly ILogger _logger;

        private static readonly HttpClient HttpClient = new HttpClient();

        private TestApiServer(IHost host, TestApiServerOptions options, ILogger logger)
        {
            Guard.NotNull(host, nameof(host), "Requires a 'IHost' instance to start/stop the test API server");
            _host = host;
            _options = options;
            _logger = logger;
        }
        
        /// <summary>
        /// Starts a new instance of the <see cref="TestApiServer"/> using the configurable <paramref name="options"/>.
        /// </summary>
        /// <param name="options">The configurable options to control the behavior of the test API server.</param>
        /// <param name="logger">The logger instance to include in the test API server to write diagnostic messages during the lifetime of the server.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="options"/> or <paramref name="logger"/> is <c>null</c>.</exception>
        public static async Task<TestApiServer> StartNewAsync(TestApiServerOptions options, ILogger logger)
        {
            Guard.NotNull(options, nameof(options), "Requires a set of configurable options to control the behavior of the test API server");
            Guard.NotNull(logger, nameof(logger), "Requires a logger instance to write diagnostic messages during the lifetime of the test API server");

            IHostBuilder builder = Host.CreateDefaultBuilder();
            options.ApplyOptions(builder);
            options.ConfigureServices(services =>
                services.AddLogging(logging => logging.AddProvider(new CustomLoggerProvider(logger))));

            IHost host = builder.Build();
            var server = new TestApiServer(host, options, logger);
            await host.StartAsync();

            return server;
        }

        /// <summary>
        /// Sends a HTTP request to the test API server based on the result of the given request <paramref name="builder"/>.
        /// </summary>
        /// <param name="builder">The builder instance to create an <see cref="HttpRequestMessage"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="builder"/> is <c>null</c>.</exception>
        public async Task<HttpResponseMessage> SendAsync(HttpRequestBuilder builder)
        {
            Guard.NotNull(builder, nameof(builder), "Requires a HTTP request builder instance to create a HTTP request to the test API server");

            HttpRequestMessage request = builder.Build(_options.Url);
            try
            {
                HttpResponseMessage response = await HttpClient.SendAsync(request);
                return response;
            }
            catch (Exception exception)
            {
                _logger.LogCritical(exception, "Cannot connect to HTTP endpoint {Method} '{Uri}'", request.Method, request.RequestUri);
                throw;
            }
        }
        
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            await _host.StopAsync();
            _host.Dispose();
        }
    }
}
