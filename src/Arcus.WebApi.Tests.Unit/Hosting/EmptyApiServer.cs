using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using GuardNet;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Arcus.WebApi.Tests.Unit.Hosting
{
    /// <summary>
    /// Represents an empty API server to test features from scratch.
    /// </summary>
    public class EmptyApiServer : WebApplicationFactory<TestStartup>
    {
        private readonly ICollection<Action<IServiceCollection>> _configureServices;
        private readonly ICollection<Action<IApplicationBuilder>> _configures;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="EmptyApiServer" /> class.
        /// </summary>
        public EmptyApiServer()
        {
            _configureServices = new Collection<Action<IServiceCollection>>();
            _configures = new Collection<Action<IApplicationBuilder>>();
        }

        /// <summary>
        /// Gives a fixture an opportunity to configure the application before it gets built.
        /// </summary>
        /// <param name="builder">The <see cref="T:Microsoft.AspNetCore.Hosting.IWebHostBuilder" /> for the application.</param>
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                foreach (Action<IServiceCollection> configureService in _configureServices)
                {
                    configureService(services);
                }
            });

            builder.Configure(app =>
            {
                foreach (Action<IApplicationBuilder> configure in _configures)
                {
                    configure(app);
                }
            });
        }

#if NETCOREAPP2_2
        /// <summary>
        /// Creates a <see cref="T:Microsoft.AspNetCore.Hosting.IWebHostBuilder" /> used to set up <see cref="T:Microsoft.AspNetCore.TestHost.TestServer" />.
        /// </summary>
        /// <remarks>
        /// The default implementation of this method looks for a <c>public static IWebHostBuilder CreateWebHostBuilder(string[] args)</c>
        /// method defined on the entry point of the assembly of <typeparamref name="TEntryPoint" /> and invokes it passing an empty string
        /// array as arguments.
        /// </remarks>
        /// <returns>A <see cref="T:Microsoft.AspNetCore.Hosting.IWebHostBuilder" /> instance.</returns>
        protected override IWebHostBuilder CreateWebHostBuilder()
        {
            return new WebHostBuilder().ConfigureKestrel(options => options.AddServerHeader = false);
        }
#endif

#if NETCOREAPP3_1
      protected override IHostBuilder CreateHostBuilder()
        {
            return Host.CreateDefaultBuilder();
        }  
#endif

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
        /// Adds a configuration of the <see cref="IServiceCollection"/> to the test server.
        /// </summary>
        public void AddServicesConfig(Action<IServiceCollection> configureServices)
        {
            Guard.NotNull(configureServices, nameof(configureServices));

            _configureServices.Add(configureServices);
        }
    }
}
