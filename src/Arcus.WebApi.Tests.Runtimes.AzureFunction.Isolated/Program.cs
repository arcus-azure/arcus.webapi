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
                    builder.UseExceptionHandling()
                           .UseFunctionContext()
                           .UseHttpCorrelation()
                           .UseRequestTracking();
                })
                .Build();
    
            host.Run();
        }
    }
}