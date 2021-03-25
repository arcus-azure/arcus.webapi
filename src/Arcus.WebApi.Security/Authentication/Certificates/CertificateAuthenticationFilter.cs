using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GuardNet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
        private const string HeaderName = "X-ARR-ClientCert";
        private const string Base64Pattern = @"^[a-zA-Z0-9\+/]*={0,3}$";
        private static readonly Regex Base64Regex = new Regex(Base64Pattern, RegexOptions.Compiled);

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
            ILogger logger = services.GetLoggerOrDefault<CertificateAuthenticationFilter>();

            if (context.ActionDescriptor?.EndpointMetadata?.Any(m => m is BypassCertificateAuthenticationAttribute || m is AllowAnonymousAttribute) == true)
            {
                logger.LogTrace("Bypass certificate authentication because '{SpecificAttribute}' or '{GeneralAttribute}' was found", nameof(BypassCertificateAuthenticationAttribute), nameof(AllowAnonymousAttribute));
                return;
            }

            var validator = services.GetService<CertificateAuthenticationValidator>();
            if (validator is null)
            {
                throw new KeyNotFoundException(
                    $"No configured {nameof(CertificateAuthenticationValidator)} instance found in the request services container. "
                    + "Please configure such an instance (ex. in the Startup) of your application");
            }

            if (TryGetClientCertificateFromRequest(context.HttpContext, logger, out X509Certificate2 clientCertificate))
            {
                bool isCertificateAllowed = await validator.IsCertificateAllowedAsync(clientCertificate, services);
                if (isCertificateAllowed)
                {
                    LogSecurityEvent(logger, "Client certificate in request is considered allowed according to configured validation requirements");
                }
                else
                {
                    LogSecurityEvent(logger, "Client certificate in request is not considered allowed according to the configured validation requirements", HttpStatusCode.Unauthorized);
                    context.Result = new UnauthorizedObjectResult("Client certificate in request is not allowed");
                }
            }
            else
            {
                LogSecurityEvent(logger, "No client certificate is specified in the request while this authentication filter requires a certificate to validate on the configured validation requirements", HttpStatusCode.Unauthorized);
                context.Result = new UnauthorizedObjectResult("No client certificate found in request");
            }
        }

        private static bool TryGetClientCertificateFromRequest(HttpContext context, ILogger logger, out X509Certificate2 clientCertificate)
        {
            if (context.Connection.ClientCertificate != null)
            {
                clientCertificate = context.Connection.ClientCertificate;
                return clientCertificate != null;
            }

            if (!context.Request.Headers.TryGetValue(HeaderName, out StringValues headerValues))
            {
                logger.LogTrace("Cannot load client certificate because request header {HeaderName} was not found", HeaderName);

                clientCertificate = null;
                return false;
            }

            try
            {
                var headerValue = headerValues.ToString();
                if (!String.IsNullOrWhiteSpace(headerValue) 
                    && headerValue.Trim().Length % 4 == 0 
                    && Base64Regex.IsMatch(headerValue))
                {
                    byte[] rawData = Convert.FromBase64String(headerValue);
                    clientCertificate = new X509Certificate2(rawData);
                    return true;
                }

                logger.LogTrace(
                    "Cannot load client certificate from request header {HeaderName} because the header value is not a valid base64 encoded string",
                    HeaderName);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Cannot load client certificate from {HeaderName} header due to an unexpected exception", HeaderName);
            }

            clientCertificate = null;
            return false;
        }

        private static void LogSecurityEvent(ILogger logger, string description, HttpStatusCode? responseStatusCode = null)
        {
            var telemetryContext = new Dictionary<string, object>
            {
                ["EventType"] = "Security",
                ["AuthenticationType"] = "Certificate",
                ["Description"] = description
            };

            if (responseStatusCode != null)
            {
                telemetryContext["StatusCode"] = responseStatusCode.ToString(); 
            }

            logger.LogSecurityEvent("Authentication", telemetryContext);
        }
    }
}
