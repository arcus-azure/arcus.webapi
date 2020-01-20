---
title: "Automatically Invalidate Azure Key Vault Secrets"
layout: default
---

# Automatically Invalidate Azure Key Vault Secrets

The `Arcus.WebApi.Jobs` library provides a background job to automatically invalidate cached Azure Key Vault secrets from an `ICachedSecretProvider` instance of your choice.

## How This Works

This automation works by subscribing on the `SecretNewVersionCreated` event of an Azure Key Vault resource and placing those events on a Azure Service Bus Topic; which we process in our background job.

To make this automation opperational, following Azure Resources has to be used:
* Azure Key Vault instance
* Azure Service Bus Topic (which subsribes on an Key Vault event subscription)

## Usage

In a `ConfigureServices` method (ex. in your `Startup.cs`), where we have access to the `IServiceCollection` instance, we can configure our background job:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // An 'ISecretProvider' implementation to access the Azure Service Bus Topic resource;
    //     this will get the 'serviceBusTopicConnectionStringSecretKey' string (configured below) and has to retrieve the connection string for the topic.
    var mySecretProvider = new MySecretProvider();
    services.AddSingleton<ISecretProvider>(serviceProvider => mySecretProvider);

    // An `ICachedSecretProvider` implementation which secret keys will automatically be invalidated.
    services.AddSingleton<ICachedSecretProvider>(serviceProvider => new MyCachedSecretProvider(mySecretProvider));

    services.AddAutoInvalidateKeyVaultSecretBackgroundJob(
        // Prefix of the Azure Service Bus Topic subscription;
        //    this allows multiple background jobs in the same application, processing the same type of events, without conflicting subscription names.
        subscriptionNamePrefix: "MyPrefix"
        
        // Connection string secret key to a Azure Service Bus Topic.
        serviceBusTopicConnectionStringSecretKey: "MySecretKeyToServiceBusTopicConnectionString");
}
```

[&larr; back](/)