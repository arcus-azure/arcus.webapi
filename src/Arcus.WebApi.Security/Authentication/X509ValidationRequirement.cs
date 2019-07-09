using System.Security.Cryptography.X509Certificates;

namespace Arcus.WebApi.Security.Authentication 
{
    /// <summary>
    /// Represents which value of the client <see cref="X509Certificate2"/> should be validated in the <see cref="CertificateAuthenticationFilter"/>.
    /// </summary>
    internal enum X509ValidationRequirement
    {
        /// <summary>
        /// Allow only certificates where the <see cref="X509Certificate.Subject"/> matches.
        /// </summary>
        SubjectName, 
        
        /// <summary>
        /// Allow only certificates where the <see cref="X509Certificate.Issuer"/> matches.
        /// </summary>
        IssuerName, 
        
        /// <summary>
        /// Allow only certificates where the <see cref="X509Certificate2.Thumbprint"/> matches.
        /// </summary>
        Thumbprint
    }
}