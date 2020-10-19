using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using GuardNet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;

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
            ILogger logger = GetLoggerOrDefault(services);

            if (context.ActionDescriptor?.EndpointMetadata?.Any(m => m is BypassCertificateAuthenticationAttribute || m is AllowAnonymousAttribute) == true)
            {
                logger.LogTrace("Bypass certificate authentication because '{SpecificAttribute}' or '{GeneralAttribute}' was found", nameof(BypassCertificateAuthenticationAttribute), nameof(AllowAnonymousAttribute));
                return;
            }

            X509Certificate2 clientCertificate = GetOrLoadClientCertificateFromRequest(context.HttpContext, logger);
            if (clientCertificate == null)
            {
                
                logger.LogWarning(
                    "No client certificate was specified in the HTTP request while this authentication filter "
                    + "requires a certificate to validate on the configured validation requirements");

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

        private static X509Certificate2 GetOrLoadClientCertificateFromRequest(HttpContext context, ILogger logger)
        {
            if (context.Connection.ClientCertificate is null)
            {
                const string headerName = "X-ARR-ClientCert";

                try
                {
                    if (context.Request.Headers.TryGetValue(headerName, out StringValues headerValue))
                    {
                        byte[] rawData = Convert.FromBase64String(headerValue);
                        return new X509Certificate2(rawData);
                    }
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Cannot load client certificate from {headerName} header", headerName);
                }
            }

            return context.Connection.ClientCertificate;
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
