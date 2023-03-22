using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Arcus.WebApi.Tests.Runtimes.AzureFunction.Isolated
{
    public class SabotageFunction
    {
        [Function("sabotage")]
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
        {
            throw new InvalidOperationException("Sabotage this endpoint!");
        }
    }
}