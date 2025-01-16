using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Arcus.WebApi.Security.Authentication.Certificates.Interfaces;

namespace Arcus.WebApi.Security.Authentication.Certificates
{
    /// <summary>
    /// Builder to create <see cref="CertificateAuthenticationConfig"/> instances.
    /// </summary>
    public class CertificateAuthenticationConfigBuilder
    {
        private readonly IDictionary<X509ValidationRequirement, (IX509ValidationLocation location, string configuredKey)> _locationAndKeyByRequirement;

        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateAuthenticationConfigBuilder"/> class.
        /// </summary>
        public CertificateAuthenticationConfigBuilder()
        {
            _locationAndKeyByRequirement = new Dictionary<X509ValidationRequirement, (IX509ValidationLocation, string)>();
        }

        /// <summary>
        /// Configures the validation for the <see cref="X509Certificate2.SubjectName"/> from a given <paramref name="location"/> using a specified <paramref name="configuredKey"/>.
        /// </summary>
        /// <param name="location">The location to retrieve the expected certificate subject name.</param>
        /// <param name="configuredKey">The configured key that the <paramref name="location"/> requires to retrieve the expected subject name.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="configuredKey"/> is blank.</exception>
        public CertificateAuthenticationConfigBuilder WithSubject(X509ValidationLocation location, string configuredKey)
            => WithSubject(GetValidationLocationImplementation(location), configuredKey);

        /// <summary>
        /// Configures the validation for the <see cref="X509Certificate2.SubjectName"/> from a given <paramref name="location"/> using a specified <paramref name="configuredKey"/>.
        /// </summary>
        /// <param name="location">The location to retrieve the expected certificate subject name.</param>
        /// <param name="configuredKey">The configured key that the <paramref name="location"/> requires to retrieve the expected subject name.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="location"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="configuredKey"/> is blank.</exception>
        public CertificateAuthenticationConfigBuilder WithSubject(IX509ValidationLocation location, string configuredKey)
            => AddCertificateRequirement(X509ValidationRequirement.SubjectName, location, configuredKey);

        /// <summary>
        /// Configures the validation for the <see cref="X509Certificate2.IssuerName"/> from a given <paramref name="location"/> using a specified <paramref name="configuredKey"/>.
        /// </summary>
        /// <param name="location">The location to retrieve the expected certificate issuer name.</param>
        /// <param name="configuredKey">The configured key that the <paramref name="location"/> requires to retrieve the expected issuer name.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="configuredKey"/> is blank.</exception>
        public CertificateAuthenticationConfigBuilder WithIssuer(X509ValidationLocation location, string configuredKey)
            => WithIssuer(GetValidationLocationImplementation(location), configuredKey);

        /// <summary>
        /// Configures the validation for the <see cref="X509Certificate2.IssuerName"/> from a given <paramref name="location"/> using a specified <paramref name="configuredKey"/>.
        /// </summary>
        /// <param name="location">The location to retrieve the expected certificate issuer name.</param>
        /// <param name="configuredKey">The configured key that the <paramref name="location"/> requires to retrieve the expected issuer name.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="location"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="configuredKey"/> is blank.</exception>
        public CertificateAuthenticationConfigBuilder WithIssuer(IX509ValidationLocation location, string configuredKey)
            => AddCertificateRequirement(X509ValidationRequirement.IssuerName, location, configuredKey);

        /// <summary>
        /// Configures the validation for the <see cref="X509Certificate2.Thumbprint"/> from a given <paramref name="location"/> using a specified <paramref name="configuredKey"/>.
        /// </summary>
        /// <param name="location">The location to retrieve the expected certificate thumbprint.</param>
        /// <param name="configuredKey">The configured key that the <paramref name="location"/> requires to retrieve the expected thumbprint.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="configuredKey"/> is blank.</exception>
        public CertificateAuthenticationConfigBuilder WithThumbprint(X509ValidationLocation location, string configuredKey)
            => WithThumbprint(GetValidationLocationImplementation(location), configuredKey);

        /// <summary>
        /// Configures the validation for the <see cref="X509Certificate2.Thumbprint"/> from a given <paramref name="location"/> using a specified <paramref name="configuredKey"/>.
        /// </summary>
        /// <param name="location">The location to retrieve the expected certificate thumbprint.</param>
        /// <param name="configuredKey">The configured key that the <paramref name="location"/> requires to retrieve the expected thumbprint.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="location"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="configuredKey"/> is blank.</exception>
        public CertificateAuthenticationConfigBuilder WithThumbprint(IX509ValidationLocation location, string configuredKey)
            => AddCertificateRequirement(X509ValidationRequirement.Thumbprint, location, configuredKey);

        private CertificateAuthenticationConfigBuilder AddCertificateRequirement(
            X509ValidationRequirement requirement,
            IX509ValidationLocation location,
            string configuredKey)
        {
            if (location is null)
            {
                throw new ArgumentNullException(nameof(location), "Location cannot be 'null'");
            }

            if (string.IsNullOrWhiteSpace(configuredKey))
            {
                throw new ArgumentException("Configured key cannot be blank", nameof(configuredKey));
            }

            // Overwrites existing requirements.
            _locationAndKeyByRequirement[requirement] = (location, configuredKey);
            
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
        /// Creates the <see cref="CertificateAuthenticationConfig"/> from the previously configured certificate requirements.
        /// </summary>
        public CertificateAuthenticationConfig Build()
        {
            if (_locationAndKeyByRequirement.Count <= 0)
            {
                throw new InvalidOperationException(
                    "Cannot build up the certificate authentication validation because there's nothing configured to be validated on the client certificate, "
                    + $"please configure the certificate validation requirements with methods like {nameof(WithThumbprint)}, {nameof(WithIssuer)}");
            }

            return new CertificateAuthenticationConfig(_locationAndKeyByRequirement);
        }
    }
}
