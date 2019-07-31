using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using GuardNet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Arcus.WebApi.Security.Authentication.Certificates
{
    /// <summary>
    /// Authentication filter to secure HTTP requests by allowing only certain values in the client certificate.
    /// </summary>
    /// <remarks>
    ///     Please make sure you register an <see cref="CertificateAuthenticationValidator"/> instance in the request services container (ex. in the Startup).
    /// </remarks>
    public class CertificateAuthenticationFilter : IAsyncAuthorizationFilter
    {
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
                    + $"requires a certificate to validate on the configured validation requirements");

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

                bool isCertificateAllowed = await validator.IsCertificateAllowedAsync(clientCertificate, services);
                if (!isCertificateAllowed)
                {
                    context.Result = new UnauthorizedResult();
                }
            }
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
    }
}
