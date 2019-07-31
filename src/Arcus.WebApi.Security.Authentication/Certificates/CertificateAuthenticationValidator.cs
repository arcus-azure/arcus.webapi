﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using GuardNet;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Arcus.WebApi.Security.Authentication.Certificates
{
    /// <summary>
    /// Represents the model that handles the certificate authentication validation via validation requirements.
    /// </summary>
    public class CertificateAuthenticationValidator
    {
        private readonly CertificateAuthenticationConfig _certificateAuthenticationConfig;

        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateAuthenticationValidator"/> class.
        /// </summary>
        /// <param name="certificateAuthenticationConfig">The authentication configuration that describes the validation process.</param>
        public CertificateAuthenticationValidator(CertificateAuthenticationConfig certificateAuthenticationConfig)
        {
            Guard.NotNull(certificateAuthenticationConfig, nameof(certificateAuthenticationConfig), "Certificate authentication configuration cannot be 'null'");

            _certificateAuthenticationConfig = certificateAuthenticationConfig;
        }

        /// <summary>
        /// Validate the specified <paramref name="clientCertificate"/> based on the configured certificate requirements
        /// using the <paramref name="services"/> to retrieve registered instances that will provide the expected certificate values.
        /// </summary>
        /// <param name="clientCertificate">The client certificate to validate.</param>
        /// <param name="services">The collection of registered services, (ex. from the request pipeline).</param>
        /// <returns>
        ///     <c>true</c> when the specified <paramref name="clientCertificate"/> is valid, <c>false</c> otherwise.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="clientCertificate"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="services"/> is <c>null</c>.</exception>
        internal async Task<bool> IsCertificateAllowedAsync(X509Certificate2 clientCertificate, IServiceProvider services)
        {
            Guard.NotNull(clientCertificate, nameof(clientCertificate), "Certificate authentication validation requires a client certificate");
            Guard.NotNull(services, nameof(services), "Certificate authentication validation requires a service object to retrieve registered services");

            ILogger logger = GetLoggerOrDefault(services);

            IDictionary<X509ValidationRequirement, ExpectedCertificateValue> expectedValuesByRequirement =
                await _certificateAuthenticationConfig.GetAllExpectedCertificateValuesAsync(services, logger);

            return expectedValuesByRequirement.All(
                keyValue => ValidateCertificateRequirement(clientCertificate, keyValue, logger));
        }

        private static ILogger GetLoggerOrDefault(IServiceProvider services)
        {
            ILogger logger = 
                services.GetService<ILoggerFactory>()
                        ?.CreateLogger<CertificateAuthenticationFilter>();

            if (logger != null)
            {
                return logger;
            }

            return NullLogger.Instance;
        }

        private static bool ValidateCertificateRequirement(
            X509Certificate2 clientCertificate, 
            KeyValuePair<X509ValidationRequirement, ExpectedCertificateValue> keyValue, 
            ILogger logger)
        {
            switch (keyValue.Key)
            {
                case X509ValidationRequirement.SubjectName:
                    return IsCertificateSubjectNameAllowed(clientCertificate, keyValue.Value, logger);
                case X509ValidationRequirement.IssuerName:
                    return IsCertificateIssuerNameAllowed(clientCertificate, keyValue.Value, logger);
                case X509ValidationRequirement.Thumbprint:
                    return IsCertificateThumbprintAllowed(clientCertificate, keyValue.Value, logger);
                default:
                    throw new ArgumentOutOfRangeException(nameof(keyValue.Key), keyValue.Key, "Unknown validation type specified");
            }
        }

        private static bool IsCertificateSubjectNameAllowed(X509Certificate2 clientCertificate, ExpectedCertificateValue expected, ILogger logger)
        {
            IEnumerable<string> certificateSubjectNames =
                (clientCertificate.Subject ?? String.Empty)
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(subject => subject.Trim());

            bool isAllowed = certificateSubjectNames.Any(subject => String.Equals(subject, expected.Value));
            if (!isAllowed)
            {
                logger.LogWarning(
                    "Client certificate authentication failed on subject: "
                    + $"no subject found (actual={String.Join(", ", certificateSubjectNames)}) in certificate that matches expected={expected}");
            }

            return isAllowed;
        }

        private static bool IsCertificateIssuerNameAllowed(X509Certificate2 clientCertificate, ExpectedCertificateValue expected, ILogger logger)
        {
            IEnumerable<string> issuerNames = 
                (clientCertificate.Issuer ?? String.Empty)
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(issuer => issuer.Trim());

            bool isAllowed = issuerNames.Any(issuer => String.Equals(issuer, expected.Value));
            if (!isAllowed)
            {
                logger.LogWarning(
                    "Client certificate authentication failed on issuer: "
                    + $"no issuer found (actual={String.Join(", ", issuerNames)}) in certificate that matches expected={expected}");
            }

            return isAllowed;
        }

        private static bool IsCertificateThumbprintAllowed(X509Certificate2 clientCertificate, ExpectedCertificateValue expected, ILogger logger)
        {
            string actual = clientCertificate.Thumbprint?.Trim();
           
            bool isAllowed = String.Equals(expected.Value, actual);
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
