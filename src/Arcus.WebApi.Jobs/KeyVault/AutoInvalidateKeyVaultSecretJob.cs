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
using Microsoft.Extensions.Options;
using Microsoft.Rest.Azure;

namespace Arcus.WebApi.Jobs.KeyVault
{
    /// <summary>
    /// Message pump implementation to automatically invalidate Azure Key Vault secrets based on the <see cref="SecretNewVersionCreated"/> emitted event.
    /// </summary>
    public class AutoInvalidateKeyVaultSecretJob : CloudEventBackgroundJob
    {
        private readonly ICachedSecretProvider _cachedSecretProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="AutoInvalidateKeyVaultSecretJob"/> class.
        /// </summary>
        /// <param name="configuration">The configuration of the application.</param>
        /// <param name="serviceProvider">The collection of services that are configured.</param>
        /// <param name="options">The options to further configure this job.</param>
        /// <param name="logger">The logger to write telemetry to.</param>
        /// <exception cref="ArgumentNullException">When the <paramref name="serviceProvider"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">When the <paramref name="serviceProvider"/> doesn't have a registered <see cref="ICachedSecretProvider"/> instance.</exception>
        public AutoInvalidateKeyVaultSecretJob(
            IConfiguration configuration,
            IServiceProvider serviceProvider,
            IOptions<CloudEventBackgroundJobOptions> options,
            ILogger logger) : base(configuration, serviceProvider, options, logger)
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
                    "Azure Key Vault job cannot map Event Grid event to CloudEvent because the event data isn't recognized as a 'SecretNewVersionCreated' schema");
            }

            await _cachedSecretProvider.InvalidateSecretAsync(secretNewVersionCreated.ObjectName);
            Logger.LogInformation("Invalidated Azure Key Vault '{SecretName}' secret in vault '{VaultName}'", secretNewVersionCreated.ObjectName, secretNewVersionCreated.VaultName);
        }
    }
}
