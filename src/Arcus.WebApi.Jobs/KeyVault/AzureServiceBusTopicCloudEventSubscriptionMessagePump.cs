using System;
using System.Text;
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
using Newtonsoft.Json.Linq;

namespace Arcus.WebApi.Jobs.KeyVault
{
    /// <summary>
    /// Representing a Azure ServiceBus Topic message pump that will create and delete a ServiceBus Topic subscription during the lifetime of the pump.
    /// </summary>
    public abstract class AzureServiceBusTopicCloudEventSubscriptionMessagePump : AzureServiceBusMessagePump<CloudEvent>
    {
        private readonly string _topicPath, _subscriptionName;
        private readonly ManagementClient _managementClient;

        private static readonly JsonEventFormatter JsonEventFormatter = new JsonEventFormatter();

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="configuration">Configuration of the application</param>
        /// <param name="serviceProvider">Collection of services that are configured</param>
        /// <param name="logger">Logger to write telemetry to</param>
        protected AzureServiceBusTopicCloudEventSubscriptionMessagePump(
            IConfiguration configuration,
            IServiceProvider serviceProvider,
            ILogger logger) : base(configuration, serviceProvider, logger)
        {
            var settings = serviceProvider.GetService<AzureServiceBusMessagePumpSettings>();
            _subscriptionName = settings.SubscriptionName;
            
            var serviceBusConnection = new ServiceBusConnectionStringBuilder(Configuration["Arcus:ServiceBus:ConnectionStringWithTopic"]);
            _topicPath = serviceBusConnection.EntityPath;

            string connectionString = serviceBusConnection.GetNamespaceConnectionString();
            _managementClient = new ManagementClient(connectionString);
        }

        /// <summary>
        /// Deserializes a raw JSON message body.
        /// </summary>
        /// <param name="rawMessageBody">Raw message body to deserialize</param>
        /// <param name="messageContext">Context concerning the message</param>
        /// <returns>Deserialized message</returns>
        protected override CloudEvent DeserializeJsonMessageBody(byte[] rawMessageBody, MessageContext messageContext)
        {
            Guard.NotNull(rawMessageBody, nameof(rawMessageBody), "Cannot deserialize raw JSON body from 'null' input");
            Guard.NotAny(rawMessageBody, nameof(rawMessageBody), "Cannot deserialize raw JSON body from empty input");

            CloudEvent cloudEvent = JsonEventFormatter.DecodeStructuredEvent(rawMessageBody);
            return cloudEvent;
        }

        /// <inheritdoc />
        protected abstract override Task ProcessMessageAsync(
            CloudEvent message,
            AzureServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken);

        /// <summary>
        /// Triggered when the application host is ready to start the service.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            Logger.LogTrace("Creating subscription '{SubscriptionName}' on topic '{TopicPath}'...", _subscriptionName, _topicPath);
            var subscriptionDescription = new SubscriptionDescription(_topicPath, _subscriptionName)
            {
                AutoDeleteOnIdle = TimeSpan.FromHours(1),
                MaxDeliveryCount = 3,
                UserMetadata = "Subscription created by Arcus in order to run integration tests"
            };
            
            var ruleDescription = new RuleDescription("Accept-All", new TrueFilter());

            await _managementClient.CreateSubscriptionAsync(subscriptionDescription, ruleDescription, cancellationToken)
                                   .ConfigureAwait(continueOnCapturedContext: false);

            Logger.LogTrace("Subscription '{SubscriptionName}' created on topic '{TopicPath}'", _subscriptionName, _topicPath);

            await base.StartAsync(cancellationToken);
        }

        /// <summary>
        /// Triggered when the application host is performing a graceful shutdown.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the shutdown process should no longer be graceful.</param>
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            Logger.LogTrace("Deleting subscription '{SubscriptionName}' on topic '{TopicPath}'...", _subscriptionName, _topicPath);
            await _managementClient.DeleteSubscriptionAsync(_topicPath, _subscriptionName, cancellationToken);
            Logger.LogTrace("Subscription '{SubscriptionName}' deleted on topic '{TopicPath}'", _subscriptionName, _topicPath);

            await base.StopAsync(cancellationToken);
        }
    }
}
