using System;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.Observability.Correlation;
using Arcus.WebApi.Logging.Core.Correlation;
using Arcus.WebApi.Tests.Integration.Logging.Fixture;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Arcus.WebApi.Tests.Integration.Logging.Controllers
{
    [ApiController]
    public class ServiceAController : ControllerBase
    {
        public const string RouteWithMessageHandler = "service-a-message-handler",
                            RouteWithExtension = "service-a-extension",
                            ServiceBUrlParameterName = "ServiceB_Url",
                            DependencyIdHeaderNameParameter = "DependencyId_HeaderName",
                            TransactionIdHeaderNameParameter = "TransactionId_HeaderName",
                            DependencyIdGenerationParameter = "DependencyId_Generation";

        private readonly HttpClient _client;
        private readonly HttpAssert _assertion;
        private readonly ILogger<ServiceAController> _logger;

        private static readonly HttpClient DefaultHttpClient = new HttpClient();

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceAController" /> class.
        /// </summary>
        public ServiceAController(IHttpClientFactory factory, HttpAssertProvider provider, ILogger<ServiceAController> logger)
        {
            _logger = logger;
            _client = factory.CreateClient("from-service-a-to-service-b");
            _assertion = provider.GetAssertion("service-a");
        }

        [HttpGet]
        [Route(RouteWithMessageHandler)]
        public async Task<IActionResult> GetWithMessageHandler([FromHeader(Name = ServiceBUrlParameterName)] string url)
        {
            _assertion.Assert(HttpContext);
            using (HttpResponseMessage response = await _client.GetAsync(url))
            {
                return StatusCode((int) response.StatusCode);
            }
        }

        [HttpGet]
        [Route(RouteWithExtension)]
        public async Task<IActionResult> GetWithExtension(
            [FromHeader(Name = ServiceBUrlParameterName)] string url,
            [FromHeader(Name = DependencyIdGenerationParameter)] string dependencyIdGeneration,
            [FromHeader(Name = DependencyIdHeaderNameParameter)] string dependencyIdHeaderName = HttpCorrelationProperties.UpstreamServiceHeaderName,
            [FromHeader(Name = TransactionIdHeaderNameParameter)] string transactionIdHeaderName = HttpCorrelationProperties.TransactionIdHeaderName)
        {
            _assertion.Assert(HttpContext);

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            var correlationAccessor = HttpContext.RequestServices.GetRequiredService<IHttpCorrelationInfoAccessor>();
            CorrelationInfo correlation = correlationAccessor.GetCorrelationInfo();

            using (HttpResponseMessage response = 
                   await DefaultHttpClient.SendAsync(request, correlation, _logger, options =>
                   {
                       options.GenerateDependencyId = () => dependencyIdGeneration ?? Guid.NewGuid().ToString();
                       options.UpstreamServiceHeaderName = dependencyIdHeaderName;
                       options.TransactionIdHeaderName = transactionIdHeaderName;
                   }))
            {
                return StatusCode((int) response.StatusCode);
            }
        }
    }
}
