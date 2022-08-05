using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.Testing.Logging;
using Bogus;
using GuardNet;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker
{
    public interface IWorker
    {

    }
}

namespace Arcus.WebApi.Tests.Integration.Fixture
{
    public class TestAzureFunctionsApiServerOptions
    {
        private readonly ICollection<Action<IServiceCollection>> _servicesConfigures = new Collection<Action<IServiceCollection>>();
        private readonly ICollection<Action<IFunctionsWorkerApplicationBuilder>> _workerConfigures = new Collection<Action<IFunctionsWorkerApplicationBuilder>>();

        private static readonly Faker BogusGenerator = new Faker();

        /// <summary>
        /// Initializes a new instance of the <see cref="TestAzureFunctionsApiServerOptions" /> class.
        /// </summary>
        public TestAzureFunctionsApiServerOptions()
        {
            Url = $"http://localhost:{/*BogusGenerator.Random.Int(4000, 4999)*/8772}/";
        }

        public string Url { get; }

        public TestAzureFunctionsApiServerOptions ConfigureWorker(
            Action<IFunctionsWorkerApplicationBuilder> configureWorker)
        {
            _workerConfigures.Add(configureWorker);
            return this;
        }

        public TestAzureFunctionsApiServerOptions ConfigureServices(Action<IServiceCollection> configureService)
        {
            _servicesConfigures.Add(configureService);
            return this;
        }

        internal void ApplyOptions(IHostBuilder builder)
        {
            builder.ConfigureAppConfiguration(config =>
            {
                string currentDirectory = Directory.GetCurrentDirectory();
                config.SetBasePath(currentDirectory);
                config.AddJsonFile("local.settings.json");
                config.AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string>("WorkerId", Guid.NewGuid().ToString()),
                    new KeyValuePair<string, string>("Host", "localhost"),
                    new KeyValuePair<string, string>("Port", "8772"),
                    new KeyValuePair<string, string>("FUNCTIONS_WORKER_RUNTIME", "dotnet-isolated"),
                    new KeyValuePair<string, string>("AzureWebJobsStorage", "")
                });
            });

            //builder.ConfigureFunctionsWorker((context, workerBuilder) =>
            //{
            //    IServiceCollection services = workerBuilder.Services;
            //    services.RemoveAt(31);

            //    foreach (Action<IFunctionsWorkerApplicationBuilder> workerConfigure in _workerConfigures)
            //    {
            //        workerConfigure(workerBuilder);
            //    }

            //    foreach (Action<IServiceCollection> configureService in _servicesConfigures)
            //    {
            //        configureService(workerBuilder.Services);
            //    }
            //}, options => { });

            builder.ConfigureServices(services =>
            {
                IFunctionsWorkerApplicationBuilder workerBuilder = services.AddFunctionsWorkerCore();
                services.RemoveAt(31);

                foreach (Action<IFunctionsWorkerApplicationBuilder> workerConfigure in _workerConfigures)
                {
                    workerConfigure(workerBuilder);
                }

                foreach (Action<IServiceCollection> configureService in _servicesConfigures)
                {
                    configureService(services);
                }

                workerBuilder.UseDefaultWorkerMiddleware();
            });
        }
    }

    public class TestAzureFunctionsApiServer : IAsyncDisposable
    {
        private readonly IHost _host;
        private readonly TestAzureFunctionsApiServerOptions _options;
        private readonly ILogger _logger;

        private static readonly HttpClient HttpClient = new HttpClient();

        /// <summary>
        /// Initializes a new instance of the <see cref="TestAzureFunctionsApiServer" /> class.
        /// </summary>
        public TestAzureFunctionsApiServer(IHost host, TestAzureFunctionsApiServerOptions options, ILogger logger)
        {
            _host = host;
            _options = options;
            _logger = logger;
        }

        public static async Task<TestAzureFunctionsApiServer> StartNewAsync(
            TestAzureFunctionsApiServerOptions options,
            ILogger logger)
        {
            IHostBuilder builder = new HostBuilder();
            //options.ConfigureServices(services =>
            //{
            //    services.AddLogging(logging =>
            //    {
            //        logging.SetMinimumLevel(LogLevel.Trace)
            //               .AddProvider(new CustomLoggerProvider(logger));
            //    });
            //});
            options.ApplyOptions(builder);

            IHost host = builder.Build();
            var server = new TestAzureFunctionsApiServer(host, options, logger);
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
