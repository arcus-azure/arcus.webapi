using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Arcus.Security.Core;
using Arcus.Security.Core.Caching;
using Arcus.WebApi.Security.Authentication.Certificates.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Arcus.WebApi.Security.Authentication.Certificates 
{
    /// <summary>
    /// Certificate location implementation to retrieve the expected <see cref="X509Certificate2"/> value from an <see cref="ISecretProvider"/>
    /// implementation that is configured on the request services pipeline.
    /// </summary>
    internal class SecretProviderValidationLocation : IX509ValidationLocation
    {
        /// <summary>
        /// Prevents an default instance of the <see cref="SecretProviderValidationLocation"/> from being created.
        /// </summary>
        private SecretProviderValidationLocation() { }

        /// <summary>
        /// Gets the singleton instance of this type.
        /// </summary>
        internal static IX509ValidationLocation Instance { get; } = new SecretProviderValidationLocation();

        /// <summary>
        /// Gets the expected value in a <see cref="X509Certificate2"/> for an <paramref name="configurationKey"/> using the specified <paramref name="services"/>.
        /// </summary>
        /// <param name="configurationKey">The configured key for which the expected certificate value is registered.</param>
        /// <param name="services">The services collections of the HTTP request pipeline to retrieve registered instances.</param>
        public async Task<string> GetExpectedCertificateValueForConfiguredKeyAsync(string configurationKey, IServiceProvider services)
        {
            if (string.IsNullOrWhiteSpace(configurationKey))
            {
                throw new ArgumentException("Configured key cannot be blank", nameof(configurationKey));
            }
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services), "Registered services cannot be 'null'");
            }


            var userDefinedSecretProvider = services.GetService<ISecretProvider>();
            if (userDefinedSecretProvider is null)
            {
                throw new InvalidOperationException(
                    "Cannot retrieve the certificate value to validate the HTTP request because no Arcus secret store was registered in the application," 
                    + $"please register the secret store with '{nameof(IHostBuilderExtensions.ConfigureSecretStore)}' on the '{nameof(IHostBuilder)}' or with 'AddSecretStore' on the '{nameof(IServiceCollection)}'," 
                    + "for more information on the Arcus secret store: https://security.arcus-azure.net/features/secret-store");
            }

            Task<string> getValueAsync = userDefinedSecretProvider.GetRawSecretAsync(configurationKey);
            if (getValueAsync is null)
            {
                return null;
            }

            return await getValueAsync;
        }
    }
}