using System;
using System.Security.Cryptography.X509Certificates;
using GuardNet;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Arcus.WebApi.Unit.Hosting
{
    /// <summary>
    /// Security configuration addition to set the TLS client certificate on every call made via the <see cref="TestApiServer"/>.
    /// </summary>
    internal class CertificateConfiguration : IStartupFilter
    {
        private readonly X509Certificate2 _clientCertificate;

        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateConfiguration"/> class.
        /// </summary>
        /// <param name="clientCertificate">The client certificate.</param>
        public CertificateConfiguration(X509Certificate2 clientCertificate)
        {
            Guard.NotNull(clientCertificate, nameof(clientCertificate));

            _clientCertificate = clientCertificate;
        }

        /// <inheritdoc />
        public  Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return builder =>
            {
                builder.Use((context, nxt) =>
                {
                    context.Connection.ClientCertificate = _clientCertificate;
                    return nxt();
                });
                next(builder);
            };
        }
    }
}
