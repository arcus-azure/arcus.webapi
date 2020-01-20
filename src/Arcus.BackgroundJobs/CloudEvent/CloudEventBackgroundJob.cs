using System;
using System.Threading;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Messaging.Pumps.ServiceBus;
using CloudNative.CloudEvents;
using GuardNet;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Arcus.BackgroundJobs.CloudEvent
{
    /// <summary>
    /// Representing a Azure Service Bus Topic message pump that will create and delete a Service Bus Topic subscription during the lifetime of the pump.
    /// </summary>
    public abstract class CloudEventBackgroundJob : AzureServiceBusMessagePump<CloudNative.CloudEvents.CloudEvent>
    {
        private static readonly JsonEventFormatter JsonEventFormatter = new JsonEventFormatter();

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudEventBackgroundJob"/> class.
        /// </summary>
        /// <param name="configuration">The configuration of the application.</param>
        /// <param name="serviceProvider">The collection of services that are configured.</param>
        /// <param name="options">The options to further configure this job.</param>
        /// <param name="logger">The logger to write telemetry to.</param>
        /// <exception cref="ArgumentNullException">When the <paramref name="serviceProvider"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">When the <paramref name="serviceProvider"/> doesn't have a registered <see cref="AzureServiceBusMessagePumpSettings"/> instance.</exception>
        protected CloudEventBackgroundJob(
            IConfiguration configuration,
            IServiceProvider serviceProvider,
            IOptions<CloudEventBackgroundJobOptions> options,
            ILogger logger) : base(configuration, serviceProvider, logger)
        {
            Guard.NotNull(
                serviceProvider,
                nameof(serviceProvider),
                $"Requires a '{nameof(IServiceProvider)}' implementation to retrieve the '{nameof(AzureServiceBusMessagePumpSettings)}'");
            Guard.NotNull(options, nameof(options), $"Requires a '{nameof(IOptions<CloudEventBackgroundJobOptions>)}' to correctly configure this job");
            Guard.For<ArgumentException>(() => options.Value is null, $"Requires a '{nameof(IOptions<CloudEventBackgroundJobOptions>)}' to correctly configure this job");

            var messagePumpSettings = serviceProvider.GetRequiredService<AzureServiceBusMessagePumpSettings>();
            Guard.NotNull<AzureServiceBusMessagePumpSettings, ArgumentException>(
                messagePumpSettings, 
                $"The '{nameof(serviceProvider)}:{serviceProvider.GetType().Name}' requires to have a non-null '{nameof(AzureServiceBusMessagePumpSettings)}' instance registered");
            
            JobId = options.Value.JobId;
        }

        /// <summary>
        /// Gets the unique identifier for this background job to distinguish this job instance in a multi-instance deployment.
        /// </summary>
        public string JobId { get; }

        /// <summary>
        /// Deserializes a raw JSON message body.
        /// </summary>
        /// <param name="rawMessageBody">Raw message body to deserialize</param>
        /// <param name="messageContext">Context concerning the message</param>
        /// <returns>Deserialized message</returns>
        protected override CloudNative.CloudEvents.CloudEvent DeserializeJsonMessageBody(byte[] rawMessageBody, MessageContext messageContext)
        {
            Guard.NotNull(rawMessageBody, nameof(rawMessageBody), "Cannot deserialize raw JSON body from 'null' input");
            Guard.NotAny(rawMessageBody, nameof(rawMessageBody), "Cannot deserialize raw JSON body from empty input");

            CloudNative.CloudEvents.CloudEvent cloudEvent = JsonEventFormatter.DecodeStructuredEvent(rawMessageBody);
            return cloudEvent;
        }

        /// <inheritdoc />
        protected abstract override Task ProcessMessageAsync(
            CloudNative.CloudEvents.CloudEvent message,
            AzureServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken);

        /// <summary>
        /// Triggered when the application host is ready to start the service.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            ServiceBusConnectionStringBuilder serviceBusConnectionString = await GetServiceBusConnectionStringAsync();

            Logger.LogTrace("[Job: {JobId}] Creating subscription '{SubscriptionName}' on topic '{TopicPath}'...", JobId, Settings.SubscriptionName, serviceBusConnectionString.EntityPath);
            var subscriptionDescription = new SubscriptionDescription(serviceBusConnectionString.EntityPath, Settings.SubscriptionName)
            {
                AutoDeleteOnIdle = TimeSpan.FromHours(1),
                MaxDeliveryCount = 3,
                UserMetadata = $"Subscription created by Arcus job: '{JobId}' to process inbound CloudEvents."
            };
            
            var ruleDescription = new RuleDescription("Accept-All", new TrueFilter());

            var serviceBusClient = new ManagementClient(serviceBusConnectionString);
            await serviceBusClient.CreateSubscriptionAsync(subscriptionDescription, ruleDescription, cancellationToken)
                                  .ConfigureAwait(continueOnCapturedContext: false);

            Logger.LogTrace("[Job: {JobId}] Subscription '{SubscriptionName}' created on topic '{TopicPath}'", JobId, Settings.SubscriptionName, serviceBusConnectionString.EntityPath);
            await serviceBusClient.CloseAsync().ConfigureAwait(continueOnCapturedContext: false);

            await base.StartAsync(cancellationToken);
        }

        /// <summary>
        /// Triggered when the application host is performing a graceful shutdown.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the shutdown process should no longer be graceful.</param>
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            ServiceBusConnectionStringBuilder serviceBusConnectionString = await GetServiceBusConnectionStringAsync();

            Logger.LogTrace("[Job: {JobId}] Deleting subscription '{SubscriptionName}' on topic '{TopicPath}'...", JobId, Settings.SubscriptionName, serviceBusConnectionString.EntityPath);
            var serviceBusClient = new ManagementClient(serviceBusConnectionString);
            await serviceBusClient.DeleteSubscriptionAsync(serviceBusConnectionString.EntityPath, Settings.SubscriptionName, cancellationToken);
            Logger.LogTrace("[Job: {JobId}] Subscription '{SubscriptionName}' deleted on topic '{TopicPath}'", JobId, Settings.SubscriptionName, serviceBusConnectionString.EntityPath);
            await serviceBusClient.CloseAsync().ConfigureAwait(continueOnCapturedContext: false);

            await base.StopAsync(cancellationToken);
        }

        private async Task<ServiceBusConnectionStringBuilder> GetServiceBusConnectionStringAsync()
        {
            Logger.LogTrace("[Job: {JobId}] Getting ServiceBus Topic connection string on topic '{TopicPath}'...", JobId, Settings.EntityName);
            string connectionString = await Settings.GetConnectionStringAsync();
            var serviceBusConnectionBuilder = new ServiceBusConnectionStringBuilder(connectionString);
            Logger.LogTrace("[JobId: {JobId}] Got ServiceBus Topic connection string on topic '{TopicPath}'", JobId, Settings.EntityName);

            return serviceBusConnectionBuilder;
        }
    }
}
