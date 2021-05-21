namespace Arcus.WebApi.Security.Authentication.Certificates
{
    /// <summary>
    /// Represents the additional consumer-configurable options to change the behavior of the <see cref="CertificateAuthenticationFilter"/>,
    /// configured either directly or indirectly via the <see cref="CertificateAuthenticationAttribute"/>.
    /// </summary>
    public class CertificateAuthenticationOptions
    {
        /// <summary>
        /// Gets or sets the flag indicating whether or not the certificate authentication should emit security events when authenticating the request.
        /// </summary>
        public bool EmitSecurityEvents { get; set; }
    }
}
