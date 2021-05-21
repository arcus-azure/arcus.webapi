namespace Arcus.WebApi.Security.Authentication.SharedAccessKey
{
    /// <summary>
    /// Represents the additional consumer-configurable options to change the behavior of the <see cref="SharedAccessKeyAuthenticationFilter"/>,
    /// configured either directly or indirectly via the <see cref="SharedAccessKeyAuthenticationAttribute"/>.
    /// </summary>
    public class SharedAccessKeyAuthenticationOptions
    {
        /// <summary>
        /// Gets or sets the flag indicating whether or not the shared access key authentication should emit security events when authenticating the request.
        /// </summary>
        public bool EmitSecurityEvents { get; set; }
    }
}
