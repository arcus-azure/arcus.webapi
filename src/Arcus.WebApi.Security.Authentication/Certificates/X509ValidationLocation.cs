using System.Security.Cryptography.X509Certificates;
using Arcus.Security.Secrets.Core.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Arcus.WebApi.Security.Authentication.Certificates
{
    /// <summary>
    /// Represents the possibilities to retrieve the expected <see cref="X509Certificate2"/> values
    /// to compare with a received HTTP request client certificate.
    /// </summary>
    public enum X509ValidationLocation
    {
        /// <summary>
        /// Specifies that the expected <see cref="X509Certificate2"/> value should be retrieved
        /// from the registered <see cref="ISecretProvider"/> implementation in the request pipeline.
        /// </summary>
        SecretProvider,

        /// <summary>
        /// Specifies that the expected <see cref="X509Certificate2"/> value should be retrieved
        /// from the <see cref="IConfiguration"/> implementation in the request pipeline.
        /// </summary>
        Configuration
    }
}
