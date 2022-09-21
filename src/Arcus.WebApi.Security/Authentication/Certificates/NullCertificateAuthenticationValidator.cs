using System.Collections.Generic;
using Arcus.WebApi.Security.Authentication.Certificates.Interfaces;

namespace Arcus.WebApi.Security.Authentication.Certificates
{
    /// <summary>
    /// Null object implementation of the <see cref="CertificateAuthenticationValidator"/> instance to mimic a 'null' value on the <see cref="CertificateAuthenticationFilter"/>.
    /// </summary>
    internal class NullCertificateAuthenticationValidator : CertificateAuthenticationValidator
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NullCertificateAuthenticationValidator" /> class.
        /// </summary>
        internal NullCertificateAuthenticationValidator() 
            : base(new CertificateAuthenticationConfig(new Dictionary<X509ValidationRequirement, (IX509ValidationLocation location, string configuredKey)>()))
        {
            
        }
    }
}