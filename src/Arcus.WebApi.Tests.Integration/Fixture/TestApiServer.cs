using System;
using System.Diagnostics;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Arcus.Testing.Logging;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.ApplicationInsights;
using Microsoft.Extensions.Options;

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

        protected TestApiServer(IHost host, TestApiServerOptions options, ILogger logger)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host), "Requires a 'IHost' instance to start/stop the test API server");
            _options = options;
            _logger = logger;

            ServiceProvider = host.Services;
        }

        /// <summary>
        /// Gets the service instance provider to provide instances of the registered services in the test API server.
        /// </summary>
        public IServiceProvider ServiceProvider { get; }
        
        /// <summary>
        /// Starts a new instance of the <see cref="TestApiServer"/> using the configurable <paramref name="options"/>.
        /// </summary>
        /// <param name="options">The configurable options to control the behavior of the test API server.</param>
        /// <param name="logger">The logger instance to include in the test API server to write diagnostic messages during the lifetime of the server.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="options"/> or <paramref name="logger"/> is <c>null</c>.</exception>
        public static async Task<TestApiServer> StartNewAsync(TestApiServerOptions options, ILogger logger)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options), "Requires a set of configurable options to control the behavior of the test API server");
            }
            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger), "Requires a logger instance to write diagnostic messages during the lifetime of the test API server");
            }

            IHostBuilder builder = Host.CreateDefaultBuilder();
            options.ApplyOptions(builder);
            options.ConfigureServices(services =>
            {
                services.Configure<ApplicationInsightsServiceOptions>(ai =>
                {
                    ai.EnableAdaptiveSampling = false;
                    ai.AddAutoCollectedMetricExtractor = false;
                    ai.EnableActiveTelemetryConfigurationSetup = false;
                    ai.EnableEventCounterCollectionModule = false;
                    ai.EnableDiagnosticsTelemetryModule = false;
                    ai.EnablePerformanceCounterCollectionModule = false;
                    ai.EnableQuickPulseMetricStream = false;
                    ai.InstrumentationKey = "ikey";
                });
                services.AddLogging(logging =>
                {
                    logging.AddFilter<ApplicationInsightsLoggerProvider>("Microsoft.AspNetCore.DataProtection.KeyManagement.XmlKeyManager", LogLevel.None);
                    logging.SetMinimumLevel(LogLevel.Trace)
                           .AddProvider(new CustomLoggerProvider(logger));
                });
            });

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
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder), "Requires a HTTP request builder instance to create a HTTP request to the test API server");
            }

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

            Activity.Current?.Stop();
            Activity.Current?.Dispose();
            Activity.Current = null;
        }
    }
}
