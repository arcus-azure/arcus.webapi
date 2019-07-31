﻿using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Arcus.WebApi.Security.Authentication.Certificates.Interfaces;
using GuardNet;

namespace Arcus.WebApi.Security.Authentication.Certificates
{
    /// <summary>
    /// Builder to create <see cref="CertificateAuthenticationConfig"/> instances.
    /// </summary>
    public class CertificateAuthenticationConfigBuilder
    {
        private readonly IDictionary<X509ValidationRequirement, (IX509ValidationLocation location, ConfiguredKey configuredKey)> _locationAndKeyByRequirement;

        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateAuthenticationConfigBuilder"/> class.
        /// </summary>
        public CertificateAuthenticationConfigBuilder()
        {
            _locationAndKeyByRequirement = new Dictionary<X509ValidationRequirement, (IX509ValidationLocation, ConfiguredKey)>();
        }

        /// <summary>
        /// Configures the validation for the <see cref="X509Certificate2.SubjectName"/> from a given <paramref name="location"/> using a specified <paramref name="configuredKey"/>.
        /// </summary>
        /// <param name="location">The location to retrieve the expected certificate subject name.</param>
        /// <param name="configuredKey">The configured key that the <paramref name="location"/> requires to retrieve the expected subject name.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="configuredKey"/> is blank.</exception>
        public CertificateAuthenticationConfigBuilder WithSubject(X509ValidationLocation location, string configuredKey)
        {
            Guard.NotNullOrWhitespace(configuredKey, nameof(configuredKey), "Configured key to retrieve expected subject cannot be blank");

            return WithSubject(GetValidationLocationImplementation(location), configuredKey);
        }

        /// <summary>
        /// Configures the validation for the <see cref="X509Certificate2.SubjectName"/> from a given <paramref name="location"/> using a specified <paramref name="configuredKey"/>.
        /// </summary>
        /// <param name="location">The location to retrieve the expected certificate subject name.</param>
        /// <param name="configuredKey">The configured key that the <paramref name="location"/> requires to retrieve the expected subject name.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="location"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="configuredKey"/> is blank.</exception>
        public CertificateAuthenticationConfigBuilder WithSubject(IX509ValidationLocation location, string configuredKey)
        {
            Guard.NotNull(location, nameof(location), "Location implementation to retrieve the expected subject cannot be 'null'");
            Guard.NotNullOrWhitespace(configuredKey, nameof(configuredKey), "Configured key to retrieve expected subject cannot be blank");

            return AddCertificateRequirement(X509ValidationRequirement.SubjectName, location, configuredKey);
        }

        /// <summary>
        /// Configures the validation for the <see cref="X509Certificate2.IssuerName"/> from a given <paramref name="location"/> using a specified <paramref name="configuredKey"/>.
        /// </summary>
        /// <param name="location">The location to retrieve the expected certificate issuer name.</param>
        /// <param name="configuredKey">The configured key that the <paramref name="location"/> requires to retrieve the expected issuer name.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="configuredKey"/> is blank.</exception>
        public CertificateAuthenticationConfigBuilder WithIssuer(X509ValidationLocation location, string configuredKey)
        {
            Guard.NotNullOrWhitespace(configuredKey, nameof(configuredKey), "Configured key to retrieve expected issuer cannot be blank");

            return WithIssuer(GetValidationLocationImplementation(location), configuredKey);
        }

        /// <summary>
        /// Configures the validation for the <see cref="X509Certificate2.IssuerName"/> from a given <paramref name="location"/> using a specified <paramref name="configuredKey"/>.
        /// </summary>
        /// <param name="location">The location to retrieve the expected certificate issuer name.</param>
        /// <param name="configuredKey">The configured key that the <paramref name="location"/> requires to retrieve the expected issuer name.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="location"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="configuredKey"/> is blank.</exception>
        public CertificateAuthenticationConfigBuilder WithIssuer(IX509ValidationLocation location, string configuredKey)
        {
            Guard.NotNull(location, nameof(location), "Location implementation to retrieve the expected issuer cannot be 'null'");
            Guard.NotNullOrWhitespace(configuredKey, nameof(configuredKey), "Configured key to retrieve expected issuer cannot be blank");

            return AddCertificateRequirement(X509ValidationRequirement.IssuerName, location, configuredKey);
        }

        /// <summary>
        /// Configures the validation for the <see cref="X509Certificate2.Thumbprint"/> from a given <paramref name="location"/> using a specified <paramref name="configuredKey"/>.
        /// </summary>
        /// <param name="location">The location to retrieve the expected certificate thumbprint.</param>
        /// <param name="configuredKey">The configured key that the <paramref name="location"/> requires to retrieve the expected thumbprint.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="configuredKey"/> is blank.</exception>
        public CertificateAuthenticationConfigBuilder WithThumbprint(X509ValidationLocation location, string configuredKey)
        {
            Guard.NotNullOrWhitespace(configuredKey, nameof(configuredKey), "Configured key to retrieve expected thumbprint cannot be blank");

            return WithThumbprint(GetValidationLocationImplementation(location), configuredKey);
        }

        /// <summary>
        /// Configures the validation for the <see cref="X509Certificate2.Thumbprint"/> from a given <paramref name="location"/> using a specified <paramref name="configuredKey"/>.
        /// </summary>
        /// <param name="location">The location to retrieve the expected certificate thumbprint.</param>
        /// <param name="configuredKey">The configured key that the <paramref name="location"/> requires to retrieve the expected thumbprint.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="location"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="configuredKey"/> is blank.</exception>
        public CertificateAuthenticationConfigBuilder WithThumbprint(IX509ValidationLocation location, string configuredKey)
        {
            Guard.NotNull(location, nameof(location), "Location implementation to retrieve the expected thumbprint cannot be 'null'");
            Guard.NotNullOrWhitespace(configuredKey, nameof(configuredKey), "Configured key to retrieve expected thumbprint cannot be blank");

            return AddCertificateRequirement(X509ValidationRequirement.Thumbprint, location, configuredKey);
        }

        private CertificateAuthenticationConfigBuilder AddCertificateRequirement(
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
        /// Creates the <see cref="CertificateAuthenticationConfig"/> from the previously configured certificate requirements.
        /// </summary>
        public CertificateAuthenticationConfig Build()
        {
            return new CertificateAuthenticationConfig(_locationAndKeyByRequirement);
        }
    }
}
