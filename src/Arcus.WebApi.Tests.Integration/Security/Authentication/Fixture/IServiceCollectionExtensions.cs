using System.Security.Cryptography.X509Certificates;
using Arcus.WebApi.Tests.Integration.Security.Authentication.Fixture;
using Microsoft.AspNetCore.Hosting;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    // ReSharper disable once InconsistentNaming
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddClientCertificate(this IServiceCollection services, X509Certificate2 clientCertificate)
        {
            return services.AddSingleton((IStartupFilter) new CertificateConfiguration(clientCertificate));
        }
    }
}
