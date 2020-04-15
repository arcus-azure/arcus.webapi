using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Arcus.Security.Core;
using Arcus.Security.Core.Caching;
using Arcus.WebApi.Security.Authentication.Certificates.Interfaces;
using GuardNet;
using Microsoft.Extensions.DependencyInjection;

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
            Guard.NotNullOrWhitespace(configurationKey, nameof(configurationKey), "Configured key cannot be blank");
            Guard.NotNull(services, nameof(services), "Registered services cannot be 'null'");

            ISecretProvider userDefinedSecretProvider = 
                services.GetService<ICachedSecretProvider>() 
                ?? services.GetService<ISecretProvider>();

            if (userDefinedSecretProvider == null)
            {
                throw new KeyNotFoundException(
                    $"No configured {nameof(ICachedSecretProvider)} or {nameof(ISecretProvider)} implementation found in the request service container. "
                    + "Please configure such an implementation (ex. in the Startup) of your application");
            }

            Task<string> getValueAsync = userDefinedSecretProvider.GetRawSecretAsync(configurationKey);
            return getValueAsync == null ? null : await getValueAsync;
        }
    }
}