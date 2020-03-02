using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

namespace Arcus.WebApi.Unit.Logging
{
    [ApiController]
    public class SaboteurController : ControllerBase
    {
        public const string RequestBodyTooLargeRoute = "logging/saboteur";

        [HttpGet]
        [Route(RequestBodyTooLargeRoute)]
        public void ThrowRequestBodyToLarge()
        {
#if NETCOREAPP2_2
            BadHttpRequestException.Throw(RequestRejectionReason.RequestBodyTooLarge, HttpMethod.Get);
#endif
        }
    }
}
