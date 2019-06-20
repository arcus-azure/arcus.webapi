using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Security.Cryptography.X509Certificates;
using GuardNet;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Arcus.WebApi.Unit.Hosting
{
    /// <summary>
    /// Representation of a HTTP server that serves as a startup point for testing web API functionality.
    /// </summary>
    public class TestApiServer : WebApplicationFactory<TestStartup>
    {
        private readonly ICollection<Action<IServiceCollection>> _addServices;
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
        public TestApiServer(Action<IServiceCollection> configureServices)
        {
            Guard.NotNull(configureServices, "Configure services cannot be 'null'");

            _addServices = new Collection<Action<IServiceCollection>> { configureServices };
            _filters = new Collection<IFilterMetadata>();
        }

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
                    services.AddSingleton<IStartupFilter>(new CertificateConfiguration(_clientCertificate));
                }

                foreach (Action<IServiceCollection> configureServices in _addServices)
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
            return new WebHostBuilder().UseStartup<TestStartup>();
        }

        /// <summary>
        /// Adds a service of type <typeparamref name="T"/> to the current dependency injection container of this hosted test server.
        /// </summary>
        /// <typeparam name="T">The type of type service.</typeparam>
        /// <param name="service">The service instance that should be registered.</param>
        public void AddService<T>(T service) where T : class
        {
            Guard.NotNull(service, "Service cannot be 'null'");

            _addServices.Add(services => services.AddScoped(_ => service));
        }

        /// <summary>
        /// Adds a filter to the current MVC setup to run on every call in this hosted test server.
        /// </summary>
        /// <param name="filter">The filter to add.</param>
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
