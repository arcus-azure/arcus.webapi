using Arcus.WebApi.Tests.Integration.Hosting.Formatting.Fixture;
using Microsoft.AspNetCore.Mvc;

namespace Arcus.WebApi.Tests.Integration.Hosting.Formatting.Controllers
{
    [ApiController]
    public class CountryController : ControllerBase
    {
        public const string GetPlainTextRoute = "api/v1/country/text",
                            GetJsonRoute = "api/v1/country/json";
        
        [HttpGet]
        [Route(GetPlainTextRoute)]
        public IActionResult GetPlainText([FromBody] string country)
        {
            return Ok(country);
        }

        [HttpGet]
        [Route(GetJsonRoute)]
        public IActionResult GetJson([FromBody] Country country)
        {
            return Ok(country);
        }
    }
}
