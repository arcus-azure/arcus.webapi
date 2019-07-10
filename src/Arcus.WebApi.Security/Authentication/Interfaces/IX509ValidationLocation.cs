using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Arcus.WebApi.Security.Authentication.Interfaces 
{
    /// <summary>
    /// Represents the function to retrieve expected values for a <see cref="X509Certificate2"/>.
    /// </summary>
    public interface IX509ValidationLocation
    {
        /// <summary>
        /// Gets the expected value in a <see cref="X509Certificate2"/> for an <paramref name="configurationKey"/> using the specified <paramref name="services"/>.
        /// </summary>
        /// <param name="configurationKey">The configured key for which the expected certificate value is registered.</param>
        /// <param name="services">The services collections of the HTTP request pipeline to retrieve registered instances.</param>
        Task<string> GetExpectedCertificateValueForConfiguredKeyAsync(string configurationKey, IServiceProvider services);
    }
}