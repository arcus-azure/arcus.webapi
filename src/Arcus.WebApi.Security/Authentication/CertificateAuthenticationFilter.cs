using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Arcus.Security.Secrets.Core.Interfaces;
using GuardNet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Arcus.WebApi.Security.Authentication
{
    /// <summary>
    /// Authentication filter to secure HTTP requests by allowing only certain values in the client certificate.
    /// </summary>
    public class CertificateAuthenticationFilter : IAsyncAuthorizationFilter
    {
        private readonly (X509ValidationRequirement requirement, string configurationKey)[] _requirements;

        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateAuthenticationFilter"/> class.
        /// </summary>
        /// <param name="requirement">The property of the client <see cref="X509Certificate2"/> to validate.</param>
        /// <param name="expectedValue">The expected value the property of the <see cref="X509Certificate2"/> should have.</param>
        public CertificateAuthenticationFilter(X509ValidationRequirement requirement, string expectedValue)
            : this((requirement, expectedValue)) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateAuthenticationFilter"/> class.
        /// </summary>
        /// <param name="requirements">The sequence of requirement property of the client <see cref="X509Certificate2"/> and expected values it should have.</param>
        public CertificateAuthenticationFilter(
            params (X509ValidationRequirement requirement, string configurationKey)[] requirements)
        {
            Guard.NotNull(requirements, nameof(requirements), "Sequence of requirements and their expected values should not be 'null'");
            Guard.For<ArgumentException>(() => requirements.Any(requirement => requirement.configurationKey is null), "Sequence of requirements cannot contain any expected value that is blank");

            _requirements = requirements;
        }

        /// <summary>
        /// Called early in the filter pipeline to confirm request is authorized.
        /// </summary>
        /// <param name="context">The <see cref="T:Microsoft.AspNetCore.Mvc.Filters.AuthorizationFilterContext" />.</param>
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            Guard.NotNull(context, nameof(context));
            Guard.NotNull(context.HttpContext, nameof(context.HttpContext));
            Guard.For<ArgumentException>(() => context.HttpContext.Connection is null, "Invalid action context given without any HTTP connection");
            Guard.For<ArgumentException>(() => context.HttpContext.RequestServices is null, "Invalid action context given without any HTTP request services");

            ILogger logger = 
                context.HttpContext.RequestServices
                       .GetService<ILoggerFactory>()
                       ?.CreateLogger<CertificateAuthenticationFilter>() 
                ?? (ILogger) NullLogger.Instance;

            ISecretProvider userDefinedSecretProvider = 
                context.HttpContext.RequestServices.GetService<ICachedSecretProvider>()
                ?? context.HttpContext.RequestServices.GetService<ISecretProvider>();

            if (userDefinedSecretProvider == null)
            {
                throw new KeyNotFoundException(
                    $"No configured {nameof(ICachedSecretProvider)} or {nameof(ISecretProvider)} implementation found in the request service container. "
                    + "Please configure such an implementation (ex. in the Startup) of your application");
            }

            X509Certificate2 clientCertificate = context.HttpContext.Connection.ClientCertificate;
            if (clientCertificate == null)
            {
                logger.LogWarning(
                    "No client certificate was specified in the HTTP request while this authentication filter "
                    + $"requires a certificate to validate on the {String.Join(", ", _requirements.Select(item => item.requirement))}");
                
                context.Result = new UnauthorizedResult();
            }
            else if (!await IsAllowedCertificate(clientCertificate, userDefinedSecretProvider, logger))
            {
                context.Result = new UnauthorizedResult();
            }
        }

        private async Task<bool> IsAllowedCertificate(
            X509Certificate2 clientCertificate, 
            ISecretProvider provider,
            ILogger logger)
        {
            var requirementValues = await Task.WhenAll(_requirements.Select(async item =>
            {
                string expected = await provider.Get(item.configurationKey);
                return (requirement: item.requirement, key: item.configurationKey, expected: expected);
            }));

            return requirementValues.All(value =>
            {
                if (value.expected == null)
                {
                    logger.LogWarning($"Client certificate authentication failed: no configuration value found for key={value.key}");
                    return false;
                }

                switch (value.requirement)
                {
                    case X509ValidationRequirement.SubjectName:
                        return IsAllowedCertificateSubject(clientCertificate, value.expected, logger);
                    case X509ValidationRequirement.IssuerName:
                        return IsAllowedCertificateIssuer(clientCertificate, value.expected, logger);
                    case X509ValidationRequirement.Thumbprint:
                        return IsAllowedCertificateThumbprint(clientCertificate, value.expected, logger);
                    default:
                        throw new ArgumentOutOfRangeException(nameof(value.requirement), value.requirement, "Unknown validation type specified");
                }
            });
        }

        private static bool IsAllowedCertificateSubject(X509Certificate2 clientCertificate, string expected, ILogger logger)
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

        private static bool IsAllowedCertificateIssuer(X509Certificate2 clientCertificate, string expected, ILogger logger)
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

        private static bool IsAllowedCertificateThumbprint(X509Certificate2 clientCertificate, string expected, ILogger logger)
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
