using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
                .ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Trace))
                .Build();
    
            host.Run();
        }
    }
}