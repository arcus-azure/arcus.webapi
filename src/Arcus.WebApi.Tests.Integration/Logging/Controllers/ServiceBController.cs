using Arcus.WebApi.Tests.Integration.Logging.Fixture;
using Microsoft.AspNetCore.Mvc;

namespace Arcus.WebApi.Tests.Integration.Logging.Controllers
{
    [ApiController]
    public class ServiceBController : ControllerBase
    {
        private readonly HttpAssert _assertion;
        public const string Route = "service-b";

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceBController" /> class.
        /// </summary>
        public ServiceBController(HttpAssertProvider provider)
        {
            _assertion = provider.GetAssertion("service-b");
        }

        [HttpGet]
        [Route(Route)]
        public IActionResult Get()
        {
            _assertion.Assert(HttpContext);
           return Ok();
        }
    }
}
