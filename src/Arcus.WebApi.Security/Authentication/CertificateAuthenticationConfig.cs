using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Arcus.WebApi.Security.Authentication.Interfaces;
using GuardNet;
using Microsoft.Extensions.Logging;

namespace Arcus.WebApi.Security.Authentication
{
    /// <summary>
    /// Representation of the configurable validation requirements on a <see cref="X509Certificate2"/>.
    /// </summary>
    public class CertificateAuthenticationConfig
    {
        private readonly IDictionary<X509ValidationRequirement, (IX509ValidationLocation location, ConfiguredKey configuredKey)> _locationAndKeyByRequirement;

        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateAuthenticationConfig"/> class.
        /// </summary>
        public CertificateAuthenticationConfig()
        {
            _locationAndKeyByRequirement = new Dictionary<X509ValidationRequirement, (IX509ValidationLocation, ConfiguredKey)>();
        }

        /// <summary>
        /// Configures the validation for the <see cref="X509Certificate2.SubjectName"/> from a given <paramref name="location"/> using a specified <paramref name="configuredKey"/>.
        /// </summary>
        /// <param name="location">The location to retrieve the expected certificate subject name.</param>
        /// <param name="configuredKey">The configured key that the <paramref name="location"/> requires to retrieve the expected subject name.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="configuredKey"/> is blank.</exception>
        public CertificateAuthenticationConfig WithSubject(X509ValidationLocation location, string configuredKey)
        {
            return WithSubject(GetValidationLocationImplementation(location), configuredKey);
        }

        /// <summary>
        /// Configures the validation for the <see cref="X509Certificate2.SubjectName"/> from a given <paramref name="location"/> using a specified <paramref name="configuredKey"/>.
        /// </summary>
        /// <param name="location">The location to retrieve the expected certificate subject name.</param>
        /// <param name="configuredKey">The configured key that the <paramref name="location"/> requires to retrieve the expected subject name.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="location"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="configuredKey"/> is blank.</exception>
        public CertificateAuthenticationConfig WithSubject(IX509ValidationLocation location, string configuredKey)
        {
            return AddCertificateRequirement(X509ValidationRequirement.SubjectName, location, configuredKey);
        }

        /// <summary>
        /// Configures the validation for the <see cref="X509Certificate2.IssuerName"/> from a given <paramref name="location"/> using a specified <paramref name="configuredKey"/>.
        /// </summary>
        /// <param name="location">The location to retrieve the expected certificate issuer name.</param>
        /// <param name="configuredKey">The configured key that the <paramref name="location"/> requires to retrieve the expected issuer name.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="configuredKey"/> is blank.</exception>
        public CertificateAuthenticationConfig WithIssuer(X509ValidationLocation location, string configuredKey)
        {
            return WithIssuer(GetValidationLocationImplementation(location), configuredKey);
        }

        /// <summary>
        /// Configures the validation for the <see cref="X509Certificate2.IssuerName"/> from a given <paramref name="location"/> using a specified <paramref name="configuredKey"/>.
        /// </summary>
        /// <param name="location">The location to retrieve the expected certificate issuer name.</param>
        /// <param name="configuredKey">The configured key that the <paramref name="location"/> requires to retrieve the expected issuer name.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="location"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="configuredKey"/> is blank.</exception>
        public CertificateAuthenticationConfig WithIssuer(IX509ValidationLocation location, string configuredKey)
        {
            return AddCertificateRequirement(X509ValidationRequirement.IssuerName, location, configuredKey);
        }

        /// <summary>
        /// Configures the validation for the <see cref="X509Certificate2.Thumbprint"/> from a given <paramref name="location"/> using a specified <paramref name="configuredKey"/>.
        /// </summary>
        /// <param name="location">The location to retrieve the expected certificate thumbprint.</param>
        /// <param name="configuredKey">The configured key that the <paramref name="location"/> requires to retrieve the expected thumbprint.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="configuredKey"/> is blank.</exception>
        public CertificateAuthenticationConfig WithThumbprint(X509ValidationLocation location, string configuredKey)
        {
            return WithThumbprint(GetValidationLocationImplementation(location), configuredKey);
        }

        /// <summary>
        /// Configures the validation for the <see cref="X509Certificate2.Thumbprint"/> from a given <paramref name="location"/> using a specified <paramref name="configuredKey"/>.
        /// </summary>
        /// <param name="location">The location to retrieve the expected certificate thumbprint.</param>
        /// <param name="configuredKey">The configured key that the <paramref name="location"/> requires to retrieve the expected thumbprint.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="location"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="configuredKey"/> is blank.</exception>
        public CertificateAuthenticationConfig WithThumbprint(IX509ValidationLocation location, string configuredKey)
        {
            return AddCertificateRequirement(X509ValidationRequirement.Thumbprint, location, configuredKey);
        }

        private CertificateAuthenticationConfig AddCertificateRequirement(
            X509ValidationRequirement requirement,
            IX509ValidationLocation location,
            string configuredKey)
        {
            Guard.NotNull(location, nameof(location), "Location cannot be 'null'");
            Guard.NotNullOrWhitespace(configuredKey, nameof(configuredKey), "Configured key cannot be blank");

            // Overwrites existing requirements.
            _locationAndKeyByRequirement[requirement] = (location, new ConfiguredKey(configuredKey));
            
            return this;
        }

        private static IX509ValidationLocation GetValidationLocationImplementation(X509ValidationLocation location)
        {
            switch (location)
            {
                case X509ValidationLocation.SecretProvider:
                    return SecretProviderValidationLocation.Instance;
                case X509ValidationLocation.Configuration:
                    return ConfigurationValidationLocation.Instance;
                default:
                    throw new ArgumentOutOfRangeException(nameof(location), location, "Unknown certificate validation location");
            }
        }

        /// <summary>
        /// Gets all the expected <see cref="X509Certificate2"/> values from this current configuration instance.
        /// </summary>
        /// <param name="services">The request services to retrieve the necessary implementations during the retrieval of each expected certificate value.</param>
        /// <param name="logger">The logger used during the retrieval of each expected certificate value.</param>
        /// <returns>The key/value pair of which certificate requirement to validate together with which value in the actual client certificate to expect.</returns>
        internal async Task<IDictionary<X509ValidationRequirement, ExpectedCertificateValue>> GetAllExpectedCertificateValues(IServiceProvider services, ILogger logger)
        {
            var expectedValuesByRequirement = await Task.WhenAll(_locationAndKeyByRequirement.Select(async keyValue =>
            {
                IX509ValidationLocation location = keyValue.Value.location;
                ConfiguredKey configuredKey = keyValue.Value.configuredKey;

                Task<string> getExpectedAsync = location.GetExpectedCertificateValueForConfiguredKey(configuredKey.Value, services);
                string expected = getExpectedAsync != null ? await getExpectedAsync : null;
                
                if (expected == null)
                {
                    logger.LogWarning($"Client certificate authentication failed: no configuration value found for key={configuredKey}");
                }

                return new KeyValuePair<X509ValidationRequirement, string>(keyValue.Key, expected);
            }));

            return expectedValuesByRequirement
                   .Where(result => result.Value != null)
                   .ToDictionary(result => result.Key, result => new ExpectedCertificateValue(result.Value));
        }
    }
}
