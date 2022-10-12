using System.Text.Json.Serialization;
using Microsoft.Extensions.Hosting;

namespace Arcus.WebApi.Tests.Runtimes.AzureFunction.Isolated
{
    public class Program
    {
        public static void Main(string[] args)
        {
            IHost host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults(builder =>
                {
                    builder.ConfigureJsonFormatting(options => options.Converters.Add(new JsonStringEnumConverter()));

                    builder.UseOnlyJsonFormatting()
                           .UseFunctionContext()
                           .UseHttpCorrelation()
                           .UseRequestTracking()
                           .UseExceptionHandling();
                })
                .Build();
    
            host.Run();
        }
    }
}