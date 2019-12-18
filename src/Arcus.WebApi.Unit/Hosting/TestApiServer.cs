using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Arcus.WebApi.OpenApi.Extensions;
using GuardNet;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Swagger;

namespace Arcus.WebApi.Unit.Hosting
{
    /// <summary>
    /// Representation of a HTTP server that serves as a startup point for testing web API functionality.
    /// </summary>
    public class TestApiServer : WebApplicationFactory<TestStartup>
    {
        private readonly IDictionary<string, string> _configurationCollection;
        private readonly ICollection<Action<IServiceCollection>> _configureServices;
        private readonly ICollection<IFilterMetadata> _filters;

        private X509Certificate2 _clientCertificate;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestApiServer"/> class.
        /// </summary>
        public TestApiServer() : this(services => { }) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestApiServer"/> class.
        /// </summary>
        /// <param name="configureServices">The action to populate the required services in the current hosted test server.</param>
        /// <exception cref="ArgumentNullException">When the <paramref name="configureServices"/> is <c>null</c>.</exception>
        public TestApiServer(Action<IServiceCollection> configureServices)
        {
            Guard.NotNull(configureServices, "Configure services cannot be 'null'");

            _configureServices = new Collection<Action<IServiceCollection>> { configureServices };
            _filters = new Collection<IFilterMetadata>();
            _configurationCollection = new Dictionary<string, string>();
        }

        /// <summary>
        /// Gives a fixture an opportunity to configure the application before it gets built.
        /// </summary>
        /// <param name="builder">The <see cref="T:Microsoft.AspNetCore.Hosting.IWebHostBuilder" /> for the application.</param>
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseStartup<TestStartup>();
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

                services.AddMvc(options =>
                {
                    foreach (IFilterMetadata filter in _filters)
                    {
                        options.Filters.Add(filter);
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
#if NETCOREAPP3_0
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
                });
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
        /// Adds a filter to the current MVC setup to run on every call in this hosted test server.
        /// </summary>
        /// <param name="filter">The filter to add.</param>
        /// <exception cref="ArgumentNullException">When the <paramref name="filter"/> is <c>null</c>.</exception>
        public void AddFilter(IFilterMetadata filter)
        {
            Guard.NotNull(filter, "Filter cannot be 'null'");

            _filters.Add(filter);
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
    }
}
