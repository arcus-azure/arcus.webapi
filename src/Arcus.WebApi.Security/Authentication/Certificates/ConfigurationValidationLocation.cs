using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Arcus.WebApi.Security.Authentication.Certificates.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Arcus.WebApi.Security.Authentication.Certificates 
{
    /// <summary>
    /// Certificate location implementation to retrieve the expected <see cref="X509Certificate2"/> value from an <see cref="IConfiguration"/>
    /// implementation that is configured on the request services pipeline.
    /// </summary>
    internal class ConfigurationValidationLocation : IX509ValidationLocation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationValidationLocation"/> class.
        /// </summary>
        private ConfigurationValidationLocation() { }

        /// <summary>
        /// Gets the singleton instance of this type.
        /// </summary>
        internal static IX509ValidationLocation Instance { get; } = new ConfigurationValidationLocation();

        /// <summary>
        /// Gets the expected value in a <see cref="X509Certificate2"/> for an <paramref name="configurationKey"/> using the specified <paramref name="services"/>.
        /// </summary>
        /// <param name="configurationKey">The configured key for which the expected certificate value is registered.</param>
        /// <param name="services">The services collections of the HTTP request pipeline to retrieve registered instances.</param>
        public Task<string> GetExpectedCertificateValueForConfiguredKeyAsync(string configurationKey, IServiceProvider services)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(configurationKey))
                {
                    throw new ArgumentException("Configured key cannot be blank", nameof(configurationKey));
                }

                if (services is null)
                {
                    throw new ArgumentNullException(nameof(services), "Registered services cannot be 'null'");
                }

                var configuration = services.GetService<IConfiguration>() ?? throw new KeyNotFoundException(
                        $"No configured {nameof(IConfiguration)} implementation found in the request service container. "
                        + "Please configure such an implementation (ex. in the Startup) of your application");
                string value = configuration[configurationKey];
                return Task.FromResult(value);
            }
            catch (Exception ex)
            {
                return Task.FromException<string>(ex);
            }
        }
    }
}