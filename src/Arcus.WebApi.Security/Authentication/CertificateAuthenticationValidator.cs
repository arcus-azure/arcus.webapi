using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Arcus.WebApi.Security.Authentication.Interfaces;
using GuardNet;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Arcus.WebApi.Security.Authentication
{
    /// <summary>
    /// Represents the model that handles the certificate authentication validation via validation requirements.
    /// </summary>
    public class CertificateAuthenticationValidator
    {
        private readonly IDictionary<X509ValidationRequirement, IX509ValidationLocation> _certificateRequirementValidations;

        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateAuthenticationValidator"/> class.
        /// </summary>
        public CertificateAuthenticationValidator() : this(new Dictionary<X509ValidationRequirement, IX509ValidationLocation>()) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateAuthenticationValidator"/> class.
        /// </summary>
        /// <param name="certificateRequirementValidations">The series of certificate validation locations by their validation requirement.</param>
        /// <exception cref="ArgumentNullException">Thrown when the certificate validation locations are <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when any of the certificate validation locations are <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="certificateRequirementValidations"/> has an unknown validation location.</exception>
        public CertificateAuthenticationValidator(IDictionary<X509ValidationRequirement, X509ValidationLocation> certificateRequirementValidations)
            : this(certificateRequirementValidations.ToDictionary(
                       requirement => requirement.Key, 
                       requirement => GetValidationLocationImplementation(requirement.Value))) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateAuthenticationValidator"/> class.
        /// </summary>
        /// <param name="certificateRequirementValidations">The series of certificate validation locations by their validation requirement.</param>
        /// <exception cref="ArgumentNullException">Thrown when the certificate validation locations are <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when any of the certificate validation locations are <c>null</c>.</exception>
        public CertificateAuthenticationValidator(IDictionary<X509ValidationRequirement, IX509ValidationLocation> certificateRequirementValidations)
        {
            Guard.NotNull(
                certificateRequirementValidations, 
                nameof(certificateRequirementValidations), 
                "Certificate authentication validation requires a series of locations by requirement.");

            Guard.For<ArgumentException>(
                () => certificateRequirementValidations.Any(requirement => requirement.Value is null), 
                "Certificate authentication requires all locations by requirement not to be 'null'");

            _certificateRequirementValidations = certificateRequirementValidations;
        }

        /// <summary>
        /// Adds an <see cref="IX509ValidationLocation"/> implementation
        /// for a specified <paramref name="requirement"/> to use when that requirement gets verified.
        /// </summary>
        /// <param name="requirement">The validation requirement to register for a <see cref="IX509ValidationLocation"/>.</param>
        /// <param name="location">The location which should be used when the specified <paramref name="requirement"/> gets verified.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="location"/> is an unknown validation location.</exception>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when the <paramref name="requirement"/> is already configured with a <see cref="IX509ValidationLocation"/> implementation.
        /// </exception>
        public CertificateAuthenticationValidator AddRequirementLocation(X509ValidationRequirement requirement, X509ValidationLocation location)
        {
            IX509ValidationLocation locationImplementation = GetValidationLocationImplementation(location);
            return AddRequirementLocation(requirement, locationImplementation);
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
        /// Adds an <see cref="IX509ValidationLocation"/> implementation
        /// for a specified <paramref name="requirement"/> to use when that requirement gets verified.
        /// </summary>
        /// <param name="requirement">The validation requirement to register for a <see cref="IX509ValidationLocation"/>.</param>
        /// <param name="location">The location which should be used when the specified <paramref name="requirement"/> gets verified.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="requirement"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when the <paramref name="requirement"/> is already configured with a <see cref="IX509ValidationLocation"/> implementation.
        /// </exception>
        public CertificateAuthenticationValidator AddRequirementLocation(X509ValidationRequirement requirement, IX509ValidationLocation location)
        {
            Guard.NotNull(location, nameof(location), "Cannot add validation location that is 'null'");

            if (_certificateRequirementValidations.ContainsKey(requirement))
            {
                throw new InvalidOperationException(
                    $"Cannot add validation location for a requirement because there already exists a location for requirement: '{requirement}'");
            }

            _certificateRequirementValidations.Add(requirement, location);
            return this;
        }

        /// <summary>
        /// Validate the specified <paramref name="clientCertificate"/> based on the given <paramref name="requirements"/>
        /// using the <paramref name="services"/> to retrieve registered instances that will provide the expected certificate values.
        /// </summary>
        /// <param name="clientCertificate">The client certificate to validate.</param>
        /// <param name="requirements">The requirements to identify which parts of the <paramref name="clientCertificate"/> should be validated.</param>
        /// <param name="services">The collection of registered services, (ex. from the request pipeline).</param>
        /// <returns>
        ///     <c>true</c> when the specified <paramref name="clientCertificate"/> is valid, <c>false</c> otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="clientCertificate"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="requirements"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="requirements"/> contains a configuration key that is <c>null</c>.</exception>
        /// <exception cref="KeyNotFoundException">
        ///     Thrown when the <paramref name="requirements"/> requires to validate a property of the <paramref name="clientCertificate"/>
        ///     for which there exists no registered <see cref="IX509ValidationLocation"/> implementation.
        /// </exception>
        internal async Task<bool> ValidateCertificate(
            X509Certificate2 clientCertificate,
            IDictionary<X509ValidationRequirement, ConfiguredKey> requirements,
            IServiceProvider services)
        {
            Guard.NotNull(clientCertificate, nameof(clientCertificate), "Certificate authentication validation requires a client certificate");
            Guard.NotNull(requirements, nameof(requirements), "Certificate authentication validation requires a series of requirements and their configured key");
            Guard.NotNull(services, nameof(services), "Certificate authentication validation requires a service object to retrieve registered services");
            Guard.For<ArgumentException>(
                () => requirements.Any(requirement => requirement.Value is null), 
                "Certificate authentication requires all configured keys in the series of requirements to be not 'null'");

            IDictionary<X509ValidationRequirement, (ConfiguredKey configuredKey, string expected)> requirementValues = 
                await GetExpectedCertificateValues(requirements, services);
            
            ILogger logger = 
                services.GetService<ILoggerFactory>()
                       ?.CreateLogger<CertificateAuthenticationValidator>() 
                ?? (ILogger) NullLogger.Instance;

            return requirementValues.All(keyValue =>
            {
                if (keyValue.Value.expected == null)
                {
                    logger.LogWarning($"Client certificate authentication failed: no configuration value found for key={keyValue.Value.configuredKey}");
                    return false;
                }

                switch (keyValue.Key)
                {
                    case X509ValidationRequirement.SubjectName:
                        return IsCertificateSubjectNameAllowed(clientCertificate, keyValue.Value.expected, logger);
                    case X509ValidationRequirement.IssuerName:
                        return IsCertificateIssuerNameAllowed(clientCertificate, keyValue.Value.expected, logger);
                    case X509ValidationRequirement.Thumbprint:
                        return IsCertificateThumbprintAllowed(clientCertificate, keyValue.Value.expected, logger);
                    default:
                        throw new ArgumentOutOfRangeException(nameof(keyValue.Key), keyValue.Key, "Unknown validation type specified");
                }
            });
        }

        private async Task<IDictionary<X509ValidationRequirement, (ConfiguredKey, string expected)>> GetExpectedCertificateValues(
            IDictionary<X509ValidationRequirement, ConfiguredKey> requirements,
            IServiceProvider services)
        {
            var requirementsWithoutLocation = 
                requirements.Where(
                    requirement => !_certificateRequirementValidations.ContainsKey(requirement.Key)
                                   || _certificateRequirementValidations[requirement.Key] is null);
            
            if (requirementsWithoutLocation.Any())
            {
                throw new KeyNotFoundException(
                    $"No configured {nameof(IX509ValidationLocation)} is configured for certificate requirements: "
                    + $"{String.Join(", ", requirementsWithoutLocation.Select(requirement => requirement.Key))}. "
                    + $"Please complete the configuration of the {nameof(CertificateAuthenticationValidator)} "
                    + "as a service  (ex. in the Startup) of your application");
            }

            var requirementValues = await Task.WhenAll(requirements.Select(async keyValue =>
            {
                IX509ValidationLocation location = _certificateRequirementValidations[keyValue.Key];
                ConfiguredKey configuredKey = keyValue.Value;

                string expected = await location.GetCertificateValueForConfiguredKey(configuredKey.Value, services);
                return new KeyValuePair<X509ValidationRequirement, (ConfiguredKey, string)>(keyValue.Key, (keyValue.Value, expected));
            }));

            return requirementValues.ToDictionary(x => x.Key, x => x.Value);
        }

        private static bool IsCertificateSubjectNameAllowed(X509Certificate2 clientCertificate, string expected, ILogger logger)
        {
            IEnumerable<string> certificateSubjectNames =
                clientCertificate.Subject
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(subject => subject.Trim());

            bool isAllowed = certificateSubjectNames.Any(subject => String.Equals(subject, expected));
            if (!isAllowed)
            {
                logger.LogWarning(
                    "Client certificate authentication failed on subject: "
                    + $"no subject found (actual={String.Join(", ", certificateSubjectNames)}) in certificate that matches expected={expected}");
            }

            return isAllowed;
        }

        private static bool IsCertificateIssuerNameAllowed(X509Certificate2 clientCertificate, string expected, ILogger logger)
        {
            IEnumerable<string> issuerNames = 
                clientCertificate.Issuer
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(issuer => issuer.Trim());

            bool isAllowed = issuerNames.Any(issuer => String.Equals(issuer, expected));
            if (!isAllowed)
            {
                logger.LogWarning(
                    "Client certificate authentication failed on issuer: "
                    + $"no issuer found (actual={String.Join(", ", issuerNames)}) in certificate that matches expected={expected}");
            }

            return isAllowed;
        }

        private static bool IsCertificateThumbprintAllowed(X509Certificate2 clientCertificate, string expected, ILogger logger)
        {
            string actual = clientCertificate.Thumbprint?.Trim();
           
            bool isAllowed = String.Equals(expected, actual);
            if (!isAllowed)
            {
                logger.LogWarning(
                    "Client certificate authentication failed on thumbprint: "
                    + $"expected={expected} <> actual={actual}");
            }

            return isAllowed;
        }
    }
}
