using System;
using Arcus.WebApi.Correlation;
using Arcus.WebApi.Logging;
using Arcus.WebApi.Telemetry.Serilog;
using Arcus.WebApi.Telemetry.Serilog.Correlation;
using Arcus.WebApi.Tests.Unit.Correlation;
using Arcus.WebApi.Tests.Unit.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;

namespace Arcus.WebApi.Tests.Unit.Hosting
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
            services.AddHttpCorrelation();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseMiddleware<ExceptionHandlingMiddleware>();
            app.UseMiddleware<TraceIdentifierMiddleware>();
            app.UseHttpCorrelation();
            app.UseSerilogRequestLogging();

            app.UseMvc();

            app.UseSwagger();
            app.UseSwaggerUI(swaggerUiOptions =>
            {
                string assemblyName = typeof(TestStartup).Assembly.GetName().Name;

                swaggerUiOptions.SwaggerEndpoint("v1/swagger.json", assemblyName);
                swaggerUiOptions.DocumentTitle = assemblyName;
            });

            //Log.Logger = CreateLoggerConfiguration(app.ApplicationServices).CreateLogger();
        }

        private static LoggerConfiguration CreateLoggerConfiguration(IServiceProvider serviceProvider)
        {
            return new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .Enrich.With(new CorrelationInfoEnricher(serviceProvider))
                .WriteTo.Console()
                .WriteTo.Sink(serviceProvider.GetRequiredService<InMemorySink>());
        }
    }
}
