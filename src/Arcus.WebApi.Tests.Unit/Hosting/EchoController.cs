using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Arcus.WebApi.Tests.Unit.Hosting
{
    [ApiController]
    public class EchoController : ControllerBase
    {
        public const string Route = "echo";

        [HttpGet]
        [Route(Route)]
        public IActionResult Get([FromBody] string body)
        {
            return Ok(body);
        }
    }
}
