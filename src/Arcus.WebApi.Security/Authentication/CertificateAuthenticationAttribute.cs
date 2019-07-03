using System;
using System.Security.Cryptography.X509Certificates;
using GuardNet;
using Microsoft.AspNetCore.Mvc;

namespace Arcus.WebApi.Security.Authentication
{
    /// <summary>
    /// Authentication filter to secure HTTP requests by allowing only certain values in the client certificate.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    public class CertificateAuthenticationAttribute : TypeFilterAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateAuthenticationAttribute"/> class.
        /// </summary>
        public CertificateAuthenticationAttribute() : base(typeof(CertificateAuthenticationFilter)) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateAuthenticationAttribute"/> class.
        /// </summary>
        /// <param name="requirement">The property of the client <see cref="X509Certificate2"/> to validate.</param>
        /// <param name="configuredKey">The configured key the property of the <see cref="X509Certificate2"/> should have.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="configuredKey"/> is <c>null</c>, empty or white-space.</exception>
        public CertificateAuthenticationAttribute(X509ValidationRequirement requirement, string configuredKey) : base(typeof(CertificateAuthenticationFilter))
        {
            Guard.NotNullOrWhitespace(configuredKey, nameof(configuredKey), "Expected value in certificate cannot be blank");

            Arguments = new object[] { requirement, configuredKey };
        }
    }
}
