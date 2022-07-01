using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Arcus.WebApi.Tests.Unit.Logging.Fixture
{
    public class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;

        /// <summary>
        /// Initializes a new instance of the <see cref="StubHttpMessageHandler" /> class.
        /// </summary>
        public StubHttpMessageHandler(HttpStatusCode statusCode)
        {
            _statusCode = statusCode;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(_statusCode));
        }
    }
}
