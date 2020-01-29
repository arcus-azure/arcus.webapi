using System;
using Arcus.WebApi.Correlation;
using Arcus.WebApi.Telemetry.Serilog;
using Arcus.WebApi.Telemetry.Serilog.Correlation;
using Arcus.WebApi.Unit.Correlation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Arcus.WebApi.Unit.Hosting
{
    /// <summary>
    /// Test representation of a "Startup" class for ASP.NET Core.
    /// </summary>
    public class TestStartup
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TestStartup"/> class.
        /// </summary>
        /// <param name="configuration">The configuration properties of the current hosted test application.</param>
        /// <exception cref="ArgumentNullException">When the <paramref name="configuration"/> is <c>null</c>.</exception>
        public TestStartup(IConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
#if NETCOREAPP2_2
            services.AddMvc();
#else
            services.AddMvc(options => options.EnableEndpointRouting = false);
#endif
            services.AddCorrelation();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseMiddleware<TraceIdentifierMiddleware>();
            app.UseCorrelation();
            app.UseSerilogRequestLogging(options => options.WithCorrelation());

            app.UseMvc();

            app.UseSwagger();
            app.UseSwaggerUI(swaggerUiOptions =>
            {
                string assemblyName = typeof(TestStartup).Assembly.GetName().Name;

                swaggerUiOptions.SwaggerEndpoint("v1/swagger.json", assemblyName);
                swaggerUiOptions.DocumentTitle = assemblyName;
            });
        }
    }
}
