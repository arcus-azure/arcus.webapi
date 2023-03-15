using Arcus.WebApi.Tests.Runtimes.AzureFunction;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Arcus.WebApi.Tests.Runtimes.AzureFunction
{
    public class Startup : FunctionsStartup
    {
        /// <summary>
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// </summary>
        /// <param name="builder">The instance to build the registered services inside the functions app.</param>
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.AddHttpCorrelation();
        }
    }
}
