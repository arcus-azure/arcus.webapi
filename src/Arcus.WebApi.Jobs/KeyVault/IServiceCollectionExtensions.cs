using Arcus.Security.Core.Caching;
using GuardNet;
using Microsoft.Extensions.DependencyInjection;

namespace Arcus.WebApi.Jobs.KeyVault
{
    /// <summary>
    /// Extensions on the <see cref="IServiceCollection"/> to make the registration of jobs more dev-friendly.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class IServiceCollectionExtensions
    {
        /// <summary>
        /// Adds a background job to the <see cref="IServiceCollection"/> to automatically invalidate cached Azure Key Vault secrets.
        /// </summary>
        /// <param name="services">The services collection to add the job to.</param>
        /// <param name="cachedSecretProvider">The cached secret provider where the secrets should be invalidated.</param>
        /// <param name="subscriptionName">The name of the Azure Service Bus subscription that will be created to receive Key Vault events.</param>
        /// <param name="serviceBusTopicConnectionStringConfigKey">The configuration key that points to the Azure Service Bus Topic connection string.</param>
        public static IServiceCollection AddAutoInvalidateKeyVaultSecretBackgroundJob(
            this IServiceCollection services,
            ICachedSecretProvider cachedSecretProvider,
            string subscriptionName,
            string serviceBusTopicConnectionStringConfigKey)
        {
            Guard.NotNull(cachedSecretProvider, nameof(cachedSecretProvider), $"Requires a '{nameof(ICachedSecretProvider)}' instance to invalidate Azure Key Vault secrets");
            Guard.NotNullOrWhitespace(subscriptionName, nameof(subscriptionName), "Requires a non-blank subscription name of the Azure Service Bus Topic subscription, to receive Key Vault events");
            Guard.NotNullOrWhitespace(serviceBusTopicConnectionStringConfigKey, nameof(serviceBusTopicConnectionStringConfigKey), "Requires a non-blank configuration key that points to a Azure Service Bus Topic");

            services.AddSingleton(serviceProvider => cachedSecretProvider);
            services.AddServiceBusTopicMessagePump<AutoInvalidateKeyVaultSecretJob>(
                subscriptionName: subscriptionName, 
                getConnectionStringFromConfigurationFunc: config => config[serviceBusTopicConnectionStringConfigKey]);

            return services;
        }
    }
}
