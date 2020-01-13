using System;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Pumps.ServiceBus;
using Arcus.Security.Core.Caching;
using CloudNative.CloudEvents;
using GuardNet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Rest.Azure;

namespace Arcus.WebApi.Jobs.KeyVault
{
    /// <summary>
    /// Message pump implementation to automatically invalidate Azure Key Vault secrets based on the <see cref="SecretNewVersionCreated"/> emitted event.
    /// </summary>
    public class AutoInvalidateKeyVaultSecretJob : AzureServiceBusCloudEventSubscriptionMessagePump
    {
        private readonly ICachedSecretProvider _cachedSecretProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="AutoInvalidateKeyVaultSecretJob"/> class.
        /// </summary>
        /// <param name="configuration">Configuration of the application</param>
        /// <param name="serviceProvider">Collection of services that are configured</param>
        /// <param name="logger">Logger to write telemetry to</param>
        /// <exception cref="ArgumentNullException">When the <paramref name="serviceProvider"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">When the <paramref name="serviceProvider"/> doesn't have a registered <see cref="ICachedSecretProvider"/> instance.</exception>
        public AutoInvalidateKeyVaultSecretJob(
            IConfiguration configuration,
            IServiceProvider serviceProvider,
            ILogger logger) : base(configuration, serviceProvider, logger)
        {
            Guard.NotNull(
                serviceProvider, 
                nameof(serviceProvider), 
                $"Requires a '{nameof(IServiceProvider)}' instance to retrieve a registered '{nameof(ICachedSecretProvider)}' instance");

            var cachedSecretProvider = serviceProvider.GetRequiredService<ICachedSecretProvider>();
            Guard.NotNull<ICachedSecretProvider, ArgumentException>(
                cachedSecretProvider,
                $"The '{nameof(serviceProvider)}:{serviceProvider.GetType().Name}' requires to have a non-null '{nameof(ICachedSecretProvider)}' instance registered");

            _cachedSecretProvider = cachedSecretProvider;
        }

        /// <inheritdoc />
        protected override async Task ProcessMessageAsync(
            CloudEvent message,
            AzureServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            Guard.NotNull(message, nameof(message), "Cannot invalidate Azure KeyVault secret from a 'null' CloudEvent");

            var secretNewVersionCreated = message.GetPayload<SecretNewVersionCreated>();
            if (secretNewVersionCreated is null)
            {
                throw new CloudException(
                    "Azure KeyVault job cannot map EventGrid event to CloudEvent because the event data isn't recognized as a 'SecretNewVersionCreated' schema");
            }

            await _cachedSecretProvider.InvalidateSecretAsync(secretNewVersionCreated.ObjectName);

            // TODO: what else can we log that would make this log entry distinguishable?
            Logger.LogInformation($"Invalidated Azure KeyVault secret in '{_cachedSecretProvider.GetType().Name}'");
        }
    }
}
