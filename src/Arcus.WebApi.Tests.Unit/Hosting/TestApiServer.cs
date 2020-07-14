using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Arcus.WebApi.Logging;
using Arcus.WebApi.Logging.Correlation;
using Arcus.WebApi.OpenApi.Extensions;
using Arcus.WebApi.Tests.Unit.Correlation;
using Arcus.WebApi.Tests.Unit.Logging;
using GuardNet;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Extensions.Hosting;
using Serilog.Extensions.Logging;
using Swashbuckle.AspNetCore.Swagger;

namespace Arcus.WebApi.Tests.Unit.Hosting
{
    /// <summary>
    /// Representation of a HTTP server that serves as a startup point for testing web API functionality.
    /// </summary>
    public class TestApiServer : WebApplicationFactory<TestStartup>
    {
        private readonly IDictionary<string, string> _configurationCollection;
        private readonly ICollection<Action<IServiceCollection>> _configureServices;
        private readonly ICollection<Action<IApplicationBuilder>> _configures;
        private readonly ICollection<Action<FilterCollection>> _filterActions;

        private X509Certificate2 _clientCertificate;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestApiServer"/> class.
        /// </summary>
        public TestApiServer() : this(new InMemorySink(), services => { }) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestApiServer"/> class.
        /// </summary>
        /// <param name="logSink">The Serilog destination logging sink.</param>
        /// <param name="configureServices">The action to populate the required services in the current hosted test server.</param>
        /// <exception cref="ArgumentNullException">When the <paramref name="configureServices"/> is <c>null</c>.</exception>
        public TestApiServer(InMemorySink logSink, Action<IServiceCollection> configureServices)
        {
            Guard.NotNull(configureServices, "Configure services cannot be 'null'");

            _configureServices = new Collection<Action<IServiceCollection>> { configureServices, services => services.AddSingleton(logSink) };
            _filterActions = new Collection<Action<FilterCollection>>();
            _configures = new Collection<Action<IApplicationBuilder>>();
            _configurationCollection = new Dictionary<string, string>();

            LogSink = logSink;
        }

        /// <summary>
        /// Gets the in-memory sink where the log events will be emitted to.
        /// </summary>
        public InMemorySink LogSink { get; }

        /// <summary>
        /// Gives a fixture an opportunity to configure the application before it gets built.
        /// </summary>
        /// <param name="builder">The <see cref="T:Microsoft.AspNetCore.Hosting.IWebHostBuilder" /> for the application.</param>
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                if (_clientCertificate != null)
                {
                    services.AddSingleton((IStartupFilter)new CertificateConfiguration(_clientCertificate));
                }

                foreach (Action<IServiceCollection> configureServices in _configureServices)
                {
                    configureServices(services);
                }

                services.AddHttpCorrelation();
                services.AddMvc(options =>
                {
                    foreach (Action<FilterCollection> filter in _filterActions)
                    {
                        filter(options.Filters);
                    }
                });

                string assemblyName = typeof(TestApiServer).Assembly.GetName().Name;

#if NETCOREAPP2_2
                var openApiInformation = new Info
                {
                    Title = assemblyName,
                    Version = "v1"
                };
#endif
#if NETCOREAPP3_1
                var openApiInformation = new OpenApiInfo
                {
                    Title = assemblyName,
                    Version = "v1"
                };
#endif

                services.AddSwaggerGen(swaggerGenerationOptions =>
                {
                    swaggerGenerationOptions.SwaggerDoc("v1", openApiInformation);
                    swaggerGenerationOptions.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, assemblyName + ".Open-Api.xml"));
                    swaggerGenerationOptions.OperationFilter<OAuthAuthorizeOperationFilter>(new object[] { new[] { "myApiScope" } });
                    swaggerGenerationOptions.OperationFilter<SharedAccessKeyAuthenticationOperationFilter>(new object[] { new [] { "myApiScope" } });
                    swaggerGenerationOptions.OperationFilter<CertificateAuthenticationOperationFilter>(new object[] { new [] { "myApiScope" } });
                });
            });

            builder.Configure(app =>
            {
                foreach (Action<IApplicationBuilder> configure in _configures)
                {
                    configure(app);
                }

                app.UseExceptionHandling();
                app.UseMiddleware<TraceIdentifierMiddleware>();

                app.UseHttpCorrelation();
                app.UseMvc();

                app.UseSwagger();
                app.UseSwaggerUI(swaggerUiOptions =>
                {
                    string assemblyName = typeof(TestStartup).Assembly.GetName().Name;

                    swaggerUiOptions.SwaggerEndpoint("v1/swagger.json", assemblyName);
                    swaggerUiOptions.DocumentTitle = assemblyName;
                });
            });

            builder.ConfigureServices(collection =>
            {
#if NETCOREAPP2_2
                collection.AddMvc();
#else
                collection.AddMvc(options => options.EnableEndpointRouting = false);
#endif
                collection.AddHttpCorrelation();

                Logger logger = null;
                collection.AddSingleton<ILoggerFactory>(services =>
                {
                    logger = new LoggerConfiguration()
                        .MinimumLevel.Debug()
                        .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                        .Enrich.FromLogContext()
                        .Enrich.WithHttpCorrelationInfo(services)
                        .WriteTo.Console()
                        .WriteTo.Sink(LogSink)
                        .CreateLogger();

                    return new SerilogLoggerFactory(logger, dispose: false);
                });

                if (logger != null)
                {
                    // This won't (and shouldn't) take ownership of the logger. 
                    collection.AddSingleton(logger);
                }

                // Registered to provide two services...
                var diagnosticContext = new DiagnosticContext(logger);

                // Consumed by e.g. middleware
                collection.AddSingleton(diagnosticContext);

                // Consumed by user code
                collection.AddSingleton<IDiagnosticContext>(diagnosticContext);
            });
        }

        /// <summary>
        /// Creates a <see cref="T:Microsoft.AspNetCore.Hosting.IWebHostBuilder" /> used to set up <see cref="T:Microsoft.AspNetCore.TestHost.TestServer" />.
        /// </summary>
        /// <remarks>
        ///     The default implementation of this method looks for a <c>public static IWebHostBuilder CreateDefaultBuilder(string[] args)</c>
        ///     method defined on the entry point of the assembly of TEntryPoint and invokes it passing an empty string
        ///     array as arguments.
        /// </remarks>
        /// <returns>A <see cref="T:Microsoft.AspNetCore.Hosting.IWebHostBuilder" /> instance.</returns>
        protected override IWebHostBuilder CreateWebHostBuilder()
        {
            return new WebHostBuilder()
                .ConfigureAppConfiguration(builder => builder.AddInMemoryCollection(_configurationCollection));
        }

        /// <summary>
        /// Adds a configuration pair (key/value) to the <see cref="IConfiguration"/> registration of the test server.
        /// </summary>
        /// <param name="key">The key of the configuration pair.</param>
        /// <param name="value">The value of the configuration pair.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="key"/>  is blank.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="value"/> is blank.</exception>
        public void AddConfigKeyValue(string key, string value)
        {
            Guard.NotNullOrWhitespace(key, nameof(key), "Configuration key cannot be blank");
            Guard.NotNullOrWhitespace(value, nameof(value), "Configuration value cannot be blank");

            if (_configurationCollection.ContainsKey(key))
            {
                throw new InvalidOperationException(
                    $"Cannot add configuration key/value pair because there already exists an entry with the key: '{key}'");
            }

            _configurationCollection.Add(key, value);
        }

        /// <summary>
        /// Adds a 'Configure' method functionality on the <see cref="IApplicationBuilder"/> instance during the creation of the hosted test server.
        /// </summary>
        /// <param name="configure">The action to execute.</param>
        public void AddConfigure(Action<IApplicationBuilder> configure)
        {
            Guard.NotNull(configure, nameof(configure), "Action cannot be 'null'");

            _configures.Add(configure);
        }

        /// <summary>
        /// Adds a service of type <typeparamref name="T"/> to the current dependency injection container of this hosted test server.
        /// </summary>
        /// <typeparam name="T">The type of type service.</typeparam>
        /// <param name="service">The service instance that should be registered.</param>
        public void AddService<T>(T service) where T : class
        {
            Guard.NotNull(service, "Service cannot be 'null'");

            _configureServices.Add(services => services.AddScoped(_ => service));
        }

        /// <summary>
        /// Adds a configuration of the <see cref="IServiceCollection"/> to the test server.
        /// </summary>
        public void AddServicesConfig(Action<IServiceCollection> configureServices)
        {
            Guard.NotNull(configureServices, nameof(configureServices));

            _configureServices.Add(configureServices);
        }

        /// <summary>
        /// Adds a filter to the current MVC setup to run on every call in this hosted test server.
        /// </summary>
        /// <param name="filter">The filter to add.</param>
        /// <exception cref="ArgumentNullException">When the <paramref name="filter"/> is <c>null</c>.</exception>
        public void AddFilter(IFilterMetadata filter)
        {
            Guard.NotNull(filter, "Filter cannot be 'null'");

            _filterActions.Add(filters => filters.Add(filter));
        }

        /// <summary>
        /// Adds a filter to the current MVC setup to run on every call in this hosted test server.
        /// </summary>
        /// <param name="filterAction">The filter to add.</param>
        /// <exception cref="ArgumentNullException">When the <paramref name="filterAction"/> is <c>null</c>.</exception>
        public void AddFilter(Action<FilterCollection> filterAction)
        {
            Guard.NotNull(filterAction, "Filter action cannot be 'null'");

            _filterActions.Add(filterAction);
        }

        /// <summary>
        /// Sets the certificate which the client will use to authenticate itself to this test server.
        /// </summary>
        /// <param name="clientCertificate">The client certificate.</param>
        public void SetClientCertificate(X509Certificate2 clientCertificate)
        {
            Guard.NotNull(clientCertificate, nameof(clientCertificate));

            _clientCertificate = clientCertificate;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <param name="disposing">
        /// <see langword="true" /> to release both managed and unmanaged resources;
        /// <see langword="false" /> to release only unmanaged resources.
        /// </param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            LogSink.Dispose();
        }
    }
}
