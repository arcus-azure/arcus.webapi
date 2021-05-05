using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Bogus;
using GuardNet;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Arcus.WebApi.Tests.Integration.Fixture
{
    /// <summary>
    /// <para>Configurable options to change the <see cref="TestApiServer"/> hosting application.</para>
    /// <para>Contains by default the endpoint routing functionality.</para>
    /// </summary>
    public class TestApiServerOptions
    {
        private readonly Faker _bogusGenerator = new Faker();
        private readonly ICollection<Action<IServiceCollection>> _configureServices = new Collection<Action<IServiceCollection>>();
        private readonly ICollection<Action<IApplicationBuilder>> _preconfigures = new Collection<Action<IApplicationBuilder>>();
        private readonly ICollection<Action<IApplicationBuilder>> _configures = new Collection<Action<IApplicationBuilder>>();
        private readonly ICollection<Action<IHostBuilder>> _hostingConfigures = new Collection<Action<IHostBuilder>>();
        private readonly ICollection<Action<IConfigurationBuilder>> _appConfigures = new Collection<Action<IConfigurationBuilder>>();

        /// <summary>
        /// Initializes a new instance of the <see cref="TestApiServerOptions" /> class.
        /// </summary>
        public TestApiServerOptions()
        {
            Url = $"http://localhost:{_bogusGenerator.Random.Int(4000, 5999)}/";
        }
        
        /// <summary>
        /// Gets the current HTTP endpoint on which the <see cref="TestApiServer"/> will be hosted.
        /// </summary>
        internal string Url { get; }
        
        /// <summary>
        /// <para>Adds a function to configure the dependency services on the test API server.</para>
        /// <para>This corresponds with the <see cref="IHostBuilder.ConfigureServices"/> call.</para>
        /// </summary>
        /// <param name="configureServices">The function to configure the dependency services.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="configureServices"/> is <c>null</c>.</exception>
        public TestApiServerOptions ConfigureServices(Action<IServiceCollection> configureServices)
        {
            Guard.NotNull(configureServices, nameof(configureServices), "Requires a function to configure the dependency services on the test API server");
            _configureServices.Add(configureServices);

            return this;
        }

        /// <summary>
        /// <para>
        ///     Adds a function to configure the startup of the application, before
        ///     the default endpoint routing <see cref="EndpointRoutingApplicationBuilderExtensions.UseRouting"/>.
        /// </para>
        /// <para>This corresponds with the <see cref="WebHostBuilderExtensions.Configure(Microsoft.AspNetCore.Hosting.IWebHostBuilder,System.Action{Microsoft.AspNetCore.Builder.IApplicationBuilder})"/>.</para>
        /// </summary>
        /// <param name="configure">The function to configure the startup of the application.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="configure"/> is <c>null</c>.</exception>
        public TestApiServerOptions PreConfigure(Action<IApplicationBuilder> configure)
        {
            Guard.NotNull(configure, nameof(configure), "Requires a function to configure the application on the test API server");
            _preconfigures.Add(configure);

            return this;
        }
        
        /// <summary>
        /// <para>
        ///     Adds a function to configure the startup of the application, after
        ///     the default endpoint routing <see cref="EndpointRoutingApplicationBuilderExtensions.UseRouting"/>.
        /// </para>
        /// <para>This corresponds with the <see cref="WebHostBuilderExtensions.Configure(IWebHostBuilder,Action{IApplicationBuilder})"/>.</para>
        /// </summary>
        /// <param name="configure">The function to configure the startup of the application.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="configure"/> is <c>null</c>.</exception>
        public TestApiServerOptions Configure(Action<IApplicationBuilder> configure)
        {
            Guard.NotNull(configure, nameof(configure), "Requires a function to configure the application on the test API server");
            _configures.Add(configure);

            return this;
        }

        /// <summary>
        /// <para>Adds a function to configure the hosting of the application.</para>
        /// <para>This corresponds with interacting with the <see cref="IHostBuilder"/> directly.</para>
        /// </summary>
        /// <param name="configure">The function to configure the hosting of the application.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="configure"/> is <c>null</c>.</exception>
        public TestApiServerOptions ConfigureHost(Action<IHostBuilder> configure)
        {
            Guard.NotNull(configure, nameof(configure), "Requires a function to configure the hosting configuration of the test API server");
            _hostingConfigures.Add(configure);

            return this;
        }

        /// <summary>
        /// <para>Adds a function to configure the application configuration.</para>
        /// <para>This corresponds with the <see cref="IHostBuilder.ConfigureAppConfiguration"/>.</para>
        /// </summary>
        /// <param name="configure">The function to configure the application configuration.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="configure"/> is <c>null</c>.</exception>
        public TestApiServerOptions ConfigureAppConfiguration(Action<IConfigurationBuilder> configure)
        {
            Guard.NotNull(configure, nameof(configure), "Requires a function to configure the application configuration of the test API server");
            _appConfigures.Add(configure);

            return this;
        }
        
        /// <summary>
        /// Apply the current state of options to the given <paramref name="builder"/>.
        /// </summary>
        /// <param name="builder">The hosting builder to apply these options to.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="builder"/> is <c>null</c>.</exception>
        internal void ApplyOptions(IHostBuilder builder)
        {
            foreach (Action<IHostBuilder> hostConfigure in _hostingConfigures)
            {
                hostConfigure(builder);
            }

            builder.ConfigureAppConfiguration(config =>
            {
                foreach (Action<IConfigurationBuilder> appConfigure in _appConfigures)
                {
                    appConfigure(config);
                }
            });

            builder.ConfigureServices(services =>
            {
                services.AddRouting()
                        .AddControllers()
                        .AddApplicationPart(typeof(TestApiServer).Assembly);
                
                foreach (Action<IServiceCollection> configureService in _configureServices)
                {
                    configureService(services);
                }
            });

            builder.ConfigureWebHostDefaults(webHost =>
            {
                webHost.ConfigureKestrel(options => { })
                       .UseUrls(Url)
                       .Configure(app =>
                       {
                           foreach (Action<IApplicationBuilder> preconfigure in _preconfigures)
                           {
                               preconfigure(app);
                           }

                           app.UseRouting();

                           foreach (Action<IApplicationBuilder> configure in _configures)
                           {
                               configure(app);
                           }

                           app.UseEndpoints(endpoints => endpoints.MapControllers());
                       });
            });
        }
    }
}