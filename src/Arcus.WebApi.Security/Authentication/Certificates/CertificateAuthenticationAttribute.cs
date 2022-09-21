using System;
using System.Collections.Generic;
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
        private readonly CertificateAuthenticationOptions _options;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateAuthenticationAttribute"/> class.
        /// </summary>
        public CertificateAuthenticationAttribute() : base(typeof(CertificateAuthenticationFilter))
        {
            _options = new CertificateAuthenticationOptions();
            Arguments = new object[] { new NullCertificateAuthenticationValidator(), _options };
        }

        /// <summary>
        /// Gets or sets the flag indicating whether or not the certificate authentication should emit security events during the authentication process of the request.
        /// </summary>
        public bool EmitSecurityEvents
        {
            get => _options.EmitSecurityEvents;
            set => _options.EmitSecurityEvents = value;
        }
    }
}
