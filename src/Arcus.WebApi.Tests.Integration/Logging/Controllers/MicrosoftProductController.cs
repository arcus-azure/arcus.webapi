using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Arcus.WebApi.Tests.Integration.Logging.Controllers
{
    [ApiController]
    [Route(Route)]
    public class MicrosoftProductController : ControllerBase
    {
        public const string Route = "microsoft/product";

        private readonly HttpClient _client;

        /// <summary>
        /// Initializes a new instance of the <see cref="MicrosoftProductController" /> class.
        /// </summary>
        public MicrosoftProductController(IHttpClientFactory clientFactory)
        {
            _client = clientFactory.CreateClient("Stock API");
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            using (HttpResponseMessage response = await _client.GetAsync(ArcusStockController.Route))
            {
                await Task.Delay(1000);
                return StatusCode((int) response.StatusCode);
            }
        }
    }
}
