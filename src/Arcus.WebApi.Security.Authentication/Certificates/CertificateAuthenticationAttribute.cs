﻿using System;
using Microsoft.AspNetCore.Mvc;

namespace Arcus.WebApi.Security.Authentication.Certificates
{
    /// <summary>
    /// Authentication filter to secure HTTP requests by allowing only certain values in the client certificate.
    /// </summary>
    /// <remarks>
    ///     Please make sure you register an <see cref="CertificateAuthenticationValidator"/> instance in the request services container (ex. in the Startup).
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class CertificateAuthenticationAttribute : TypeFilterAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateAuthenticationAttribute"/> class.
        /// </summary>
        public CertificateAuthenticationAttribute() : base(typeof(CertificateAuthenticationFilter)) { }
    }
}
