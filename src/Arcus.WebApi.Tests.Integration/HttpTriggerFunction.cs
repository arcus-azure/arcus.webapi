using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Arcus.WebApi.Tests.Integration
{
    public class HttpTriggerFunction
    {
        [Function("test")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", "get")]
            HttpRequestData request)
        {
            return request.CreateResponse(HttpStatusCode.OK);
        }
    }
}
