using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Arcus.WebApi.Security.Authentication.SharedAccessKey;
using Microsoft.AspNetCore.Mvc;

namespace Arcus.WebApi.Tests.Unit.Security.Authentication
{
    [ApiController]
    [BypassSharedAccessKeyAuthentication]
    public class BypassSharedAccessKeyController : ControllerBase
    {
        public const string BypassOverAuthenticationRoute = "authz/bypass/over/sharedaccesskey",
                            SecretName = "MySecret",
                            HeaderName = "x-shared-access-key";

        [HttpGet]
        [Route(BypassOverAuthenticationRoute)]
        [SharedAccessKeyAuthentication(SecretName, HeaderName)]
        public IActionResult BypassOverAuthentication()
        {
            return Ok();
        }
    }
}
