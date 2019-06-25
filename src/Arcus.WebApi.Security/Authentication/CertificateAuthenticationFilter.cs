using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using GuardNet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
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
        private readonly IDictionary<X509ValidationRequirement, ConfiguredKey> _requirements;

        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateAuthenticationFilter"/> class.
        /// </summary>
        /// <param name="requirement">The property of the client <see cref="X509Certificate2"/> to validate.</param>
        /// <param name="expectedValue">The expected value the property of the <see cref="X509Certificate2"/> should have.</param>
        public CertificateAuthenticationFilter(X509ValidationRequirement requirement, string expectedValue)
            : this(new Dictionary<X509ValidationRequirement, string> { [requirement] = expectedValue }) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateAuthenticationFilter"/> class.
        /// </summary>
        public CertificateAuthenticationFilter(IDictionary<X509ValidationRequirement, string> requirements)
        {
            Guard.NotNull(requirements, nameof(requirements), "Sequence of requirements and their expected values should not be 'null'");
            Guard.For<ArgumentException>(() => requirements.Any(requirement => String.IsNullOrWhiteSpace(requirement.Value)), "Sequence of requirements cannot contain any configuration key that is blank");

            _requirements = requirements.ToDictionary(requirement => requirement.Key, requirement => new ConfiguredKey(requirement.Value));
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

            X509Certificate2 clientCertificate = context.HttpContext.Connection.ClientCertificate;
            if (clientCertificate == null)
            {
                ILogger logger = 
                    context.HttpContext.RequestServices
                           .GetService<ILoggerFactory>()
                           ?.CreateLogger<CertificateAuthenticationFilter>() 
                    ?? (ILogger) NullLogger.Instance;

                logger.LogWarning(
                    "No client certificate was specified in the HTTP request while this authentication filter "
                    + $"requires a certificate to validate on the {String.Join(", ", _requirements.Select(item => item.Key))}");
                
                context.Result = new UnauthorizedResult();
            }
            else
            {
                var validator = context.HttpContext.RequestServices.GetService<CertificateAuthenticationValidator>();
                if (validator == null)
                {
                    throw new KeyNotFoundException(
                        $"No configured {nameof(CertificateAuthenticationValidator)} instance found in the request services container. "
                        + "Please configure such an instance (ex. in the Startup) of your application");
                }

                bool isCertificateAllowed = await validator.ValidateCertificate(clientCertificate, _requirements, context.HttpContext.RequestServices);
                if (!isCertificateAllowed)
                {
                    context.Result = new UnauthorizedResult();
                }
            }
        }
    }
}
