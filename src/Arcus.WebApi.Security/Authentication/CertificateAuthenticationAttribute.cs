using System;
using Microsoft.AspNetCore.Mvc;

namespace Arcus.WebApi.Security.Authentication
{
    /// <summary>
    /// Authentication filter to secure HTTP requests by allowing only certain values in the client certificate.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class CertificateAuthenticationAttribute : TypeFilterAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateAuthenticationAttribute"/> class.
        /// </summary>
        public CertificateAuthenticationAttribute() : base(typeof(CertificateAuthenticationFilter)) { }
    }
}
