using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Arcus.WebApi.Security.Authentication.Certificates.Interfaces;
using Microsoft.Extensions.Logging;

namespace Arcus.WebApi.Security.Authentication.Certificates
{
    /// <summary>
    /// Representation of the configurable validation requirements on a <see cref="X509Certificate2"/>.
    /// </summary>
    public class CertificateAuthenticationConfig
    {
        private readonly IDictionary<X509ValidationRequirement, (IX509ValidationLocation location, string configuredKey)> _locationAndKeyByRequirement;

        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateAuthenticationConfig"/> class.
        /// </summary>
        /// <param name="locationAndKeyByRequirement">
        ///     The series of validation locations and configured keys by certificate requirements that describes how which parts of the client certificate should be validated.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="locationAndKeyByRequirement"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="locationAndKeyByRequirement"/> contains any validation location or configured key that is <c>null</c>.</exception>
        internal CertificateAuthenticationConfig(
            IDictionary<X509ValidationRequirement, (IX509ValidationLocation location, string configuredKey)> locationAndKeyByRequirement)
        {
            if (locationAndKeyByRequirement is null)
            {
                throw new ArgumentNullException(nameof(locationAndKeyByRequirement), "Location and key by certificate requirement dictionary cannot be 'null'");
            }

            if (locationAndKeyByRequirement.Any(keyValue => keyValue.Value.location is null || keyValue.Value.configuredKey is null))
            {
                throw new ArgumentException("All locations and configured keys by certificate requirement cannot be 'null'");
            }

            _locationAndKeyByRequirement = locationAndKeyByRequirement;
        }

        /// <summary>
        /// Gets all the expected <see cref="X509Certificate2"/> values from this current configuration instance.
        /// </summary>
        /// <param name="services">The request services to retrieve the necessary implementations during the retrieval of each expected certificate value.</param>
        /// <param name="logger">The logger used during the retrieval of each expected certificate value.</param>
        /// <returns>The key/value pair of which certificate requirement to validate together with which value in the actual client certificate to expect.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="logger"/> is <c>null</c>.</exception>
        internal async Task<IDictionary<X509ValidationRequirement, string>> GetAllExpectedCertificateValuesAsync(IServiceProvider services, ILogger logger)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services), "Request services cannot be 'null'");
            }

            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger), "Logger cannot be 'null'");
            }

            var expectedValuesByRequirement = 
                await Task.WhenAll(
                    _locationAndKeyByRequirement.Select(
                        keyValue => GetExpectedValueForCertificateRequirementAsync(keyValue, services, logger)));

            return expectedValuesByRequirement
                   .Where(result => result.Value != null)
                   .ToDictionary(result => result.Key, result => result.Value);
        }

        private static async Task<KeyValuePair<X509ValidationRequirement, string>> GetExpectedValueForCertificateRequirementAsync(
            KeyValuePair<X509ValidationRequirement, (IX509ValidationLocation location, string configuredKey)> keyValue, 
            IServiceProvider services, 
            ILogger logger)
        {
            IX509ValidationLocation location = keyValue.Value.location;
            string configuredKey = keyValue.Value.configuredKey;

            Task<string> getExpectedAsync = location.GetExpectedCertificateValueForConfiguredKeyAsync(configuredKey, services);
            string expected = getExpectedAsync != null ? await getExpectedAsync : null;

            if (expected is null)
            {
                logger.LogWarning("Client certificate authentication failed: no configuration value found for key={ConfiguredKey}", configuredKey);
            }

            return new KeyValuePair<X509ValidationRequirement, string>(keyValue.Key, expected);
        }
    }
}
