using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using GuardNet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Arcus.WebApi.Security.Authentication
{
    /// <summary>
    /// Authentication filter to secure HTTP requests by allowing only certain values in the client certificate.
    /// </summary>
    public class CertificateAuthenticationFilter : IAuthorizationFilter
    {
        private readonly (X509ValidationRequirement requirement, string expectedValue)[] _requirements;

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
            params (X509ValidationRequirement requirement, string expectedValue)[] requirements)
        {
            Guard.NotNull(requirements, nameof(requirements), "Sequence of requirements and their expected values should not be 'null'");
            Guard.For<ArgumentException>(() => requirements.Any(requirement => requirement.expectedValue is null), "Sequence of requirements cannot contain any expected value that is blank");

            _requirements = requirements;
        }

        /// <summary>
        /// Called early in the filter pipeline to confirm request is authorized.
        /// </summary>
        /// <param name="context">The <see cref="T:Microsoft.AspNetCore.Mvc.Filters.AuthorizationFilterContext" />.</param>
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            Guard.NotNull(context, nameof(context));
            Guard.NotNull(context.HttpContext, nameof(context.HttpContext));
            Guard.For<ArgumentException>(() => context.HttpContext.Connection == null, "Invalid action context given without any HTTP connection");

            X509Certificate2 clientCertificate = context.HttpContext.Connection.ClientCertificate;
            if (clientCertificate == null || !IsAllowedCertificate(clientCertificate))
            {
                context.Result = new UnauthorizedResult();
            }
        }

        private bool IsAllowedCertificate(X509Certificate2 clientCertificate)
        {
            return _requirements.All(item =>
            {
                switch (item.requirement)
                {
                    case X509ValidationRequirement.SubjectName:
                        return IsAllowedCertificateSubject(clientCertificate, item.expectedValue);
                    case X509ValidationRequirement.IssuerName:
                        return IsAllowedCertificateIssuer(clientCertificate, item.expectedValue);
                    case X509ValidationRequirement.Thumbprint:
                        return IsAllowedCertificateThumbprint(clientCertificate, item.expectedValue);
                    default:
                        throw new ArgumentOutOfRangeException(
                            nameof(item.requirement),
                            item.requirement,
                            "Unknown validation type specified");
                }
            });
        }

        private static bool IsAllowedCertificateSubject(X509Certificate2 clientCertificate, string expectedValue)
        {
            IEnumerable<string> certificateSubjectNames =
                clientCertificate.Subject
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(subject => subject.Trim());

            return certificateSubjectNames.Any(subject => String.Equals(subject, expectedValue));
        }

        private static bool IsAllowedCertificateIssuer(X509Certificate2 clientCertificate, string expectedValue)
        {
            IEnumerable<string> issuerNames = 
                clientCertificate.Issuer
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(issuer => issuer.Trim());

            return issuerNames.Any(issuer => String.Equals(issuer, expectedValue));
        }

        private static bool IsAllowedCertificateThumbprint(X509Certificate2 clientCertificate, string expectedValue)
        {
            return String.Equals(
                expectedValue,
                clientCertificate.Thumbprint?.Trim());
        }
    }
}
