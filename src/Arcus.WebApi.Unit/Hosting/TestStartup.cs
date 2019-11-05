using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseMvc();

            app.UseSwagger();
            app.UseSwaggerUI(swaggerUiOptions =>
            {
                swaggerUiOptions.SwaggerEndpoint("v1/swagger.json", typeof(TestStartup).Assembly.GetName().Name);
                swaggerUiOptions.DocumentTitle = typeof(TestStartup).Assembly.GetName().Name;
            });
        }
    }
}
