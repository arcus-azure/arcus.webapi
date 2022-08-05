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
                    builder.UseFunctionContext()
                           .UseHttpCorrelation();
                })
                .Build();
    
            host.Run();
        }
    }
}