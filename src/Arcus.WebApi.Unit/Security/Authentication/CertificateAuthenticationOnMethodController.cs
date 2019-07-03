﻿using System.Net.Http;
using System.Threading.Tasks;
using Arcus.WebApi.Security.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace Arcus.WebApi.Unit.Security.Authentication
{
    [ApiController]
    public class CertificateAuthenticationOnMethodController : ControllerBase
    {
        public const string AuthorizedRoute = "authz/certificate";

        [HttpGet]
        [Route(AuthorizedRoute)]
        [CertificateAuthentication]
        public Task<IActionResult> TestCertificateAuthentication(HttpRequestMessage message)
        {
            return Task.FromResult<IActionResult>(Ok());
        }
    }
}
