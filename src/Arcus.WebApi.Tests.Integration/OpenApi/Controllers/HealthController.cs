using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Arcus.WebApi.Tests.Integration.OpenApi.Controllers
{
    [ApiController]
    [Route(GetRoute)]
    public class HealthController : ControllerBase
    {
        public const string GetRoute = "api/v1/health";

        private readonly HealthCheckService _healthService;

        /// <summary>
        /// Initializes a new instance of the <see cref="HealthController" /> class.
        /// </summary>
        public HealthController(HealthCheckService healthService)
        {
            _healthService = healthService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(HealthReportJson), 200)]
        public async Task<IActionResult> Get()
        {
            HealthReportJson report = await _healthService.CheckHealthAsync();

            return Ok(report);
        }
    }
}
