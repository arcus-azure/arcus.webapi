---
title: "Automatically Invalidate Azure Key Vault Secrets"
layout: default
---

# Automatically Invalidate Azure Key Vault Secrets

The `Arcus.WebApi.Jobs` library provides a background job to automatically invalidate cached Azure Key Vault secrets from an `ICachedSecretProvider` instance of your choice.
This works by subscribing on the `SecretNewVersionCreated` event of an Azure Key Vault resource and placing those events on a Azure Service Bus Topic; which we process in our background job.

To make this automation opperational, following Azure Resources has to be used:
* Azure Key Vault instance
* Azure Service Bus Topic (which subsribes on an Key Vault event subscription)
* Azure Event Grid subscription for `SecretNewVersionCreated` events that are sent to the Azure Service Bus Topic

## Usage

Our background job has to be configured in `ConfigureServices` method:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // An 'ISecretProvider' implementation to access the Azure Service Bus Topic resource;
    //     this will get the 'serviceBusTopicConnectionStringSecretKey' string (configured below) and has to retrieve the connection string for the topic.
    var mySecretProvider = new MySecretProvider();
    services.AddSingleton<ISecretProvider>(serviceProvider => mySecretProvider);

    // An `ICachedSecretProvider` implementation which secret keys will automatically be invalidated;
    //     the `.InvalidateSecret()` method on this instance will automatically be called when an `SecretNewVersionCreated` event is emitted by the Azure Key Vault resource.
    services.AddSingleton<ICachedSecretProvider>(serviceProvider => new MyCachedSecretProvider(mySecretProvider));

    services.AddAutoInvalidateKeyVaultSecretBackgroundJob(
        // Prefix of the Azure Service Bus Topic subscription;
        //    this allows the background jobs to support applications that are running multiple instances, processing the same type of events, without conflicting subscription names.
        subscriptionNamePrefix: "MyPrefix"
        
        // Connection string secret key to the Azure Service Bus Topic that contains the Azure Key Vault events;
        //    connection string scoped to the entity path (Topic), so we can process messages on to the topic.
        serviceBusTopicConnectionStringSecretKey: "MySecretKeyToServiceBusTopicConnectionString");
}
```

[&larr; back](/)
