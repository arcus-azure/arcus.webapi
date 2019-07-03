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
        private readonly IDictionary<X509ValidationRequirement, ConfiguredKey> _configuredKeysByRequirement;

        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateAuthenticationFilter"/> class.
        /// </summary>
        /// <param name="requirement">The property of the client <see cref="X509Certificate2"/> to validate.</param>
        /// <param name="configuredKey">The configured key to retrieve the expected value of the <see cref="X509Certificate2"/> property.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="configuredKey"/> is <c>null</c>.</exception>
        public CertificateAuthenticationFilter(X509ValidationRequirement requirement, string configuredKey)
            : this(new Dictionary<X509ValidationRequirement, string> { [requirement] = configuredKey }) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateAuthenticationFilter"/> class.
        /// </summary>
        /// <param name="configuredKeysByRequirement">The series of configured keys with their requirement/property of the client <see cref="X509Certificate2"/> to validate.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="configuredKeysByRequirement"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="configuredKeysByRequirement"/> contains any configured key that is <c>null</c>.</exception>
        public CertificateAuthenticationFilter(IDictionary<X509ValidationRequirement, string> configuredKeysByRequirement)
        {
            Guard.NotNull(configuredKeysByRequirement, nameof(configuredKeysByRequirement), "Sequence of requirements and their expected values should not be 'null'");
            Guard.For<ArgumentException>(() => configuredKeysByRequirement.Any(requirement => String.IsNullOrWhiteSpace(requirement.Value)), "Sequence of requirements cannot contain any configuration key that is blank");

            _configuredKeysByRequirement = configuredKeysByRequirement.ToDictionary(requirement => requirement.Key, requirement => new ConfiguredKey(requirement.Value));
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

            IServiceProvider services = context.HttpContext.RequestServices;

            X509Certificate2 clientCertificate = context.HttpContext.Connection.ClientCertificate;
            if (clientCertificate == null)
            {
                ILogger logger = GetLoggerOrDefault(services);
                logger.LogWarning(
                    "No client certificate was specified in the HTTP request while this authentication filter "
                    + $"requires a certificate to validate on the {String.Join(", ", _configuredKeysByRequirement.Select(item => item.Key))}");

                context.Result = new UnauthorizedResult();
            }
            else
            {
                var validator = services.GetService<CertificateAuthenticationValidator>();
                if (validator == null)
                {
                    throw new KeyNotFoundException(
                        $"No configured {nameof(CertificateAuthenticationValidator)} instance found in the request services container. "
                        + "Please configure such an instance (ex. in the Startup) of your application");
                }

                bool isCertificateAllowed = await validator.ValidateCertificate(clientCertificate, _configuredKeysByRequirement, services);
                if (!isCertificateAllowed)
                {
                    context.Result = new UnauthorizedResult();
                }
            }
        }

        private static ILogger GetLoggerOrDefault(IServiceProvider services)
        {
            return services.GetService<ILoggerFactory>()
                           ?.CreateLogger<CertificateAuthenticationFilter>()
                   ?? (ILogger) NullLogger.Instance;
        }
    }
}
