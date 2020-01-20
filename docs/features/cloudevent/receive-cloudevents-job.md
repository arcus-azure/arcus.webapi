---
title: "Securely Receive CloudEvents"
layout: default
---

# Securely Receive CloudEvents

The `Arcus.BackgroundJobs` library provides a background job to securely receive [CloudEvent](https://github.com/cloudevents/spec)s.
This allows API's to have asynchronous jobs that allow external parties to forward events to react upon, without exposing a public endpoint.

## How This Works

An Azure Service Bus Topic resource is required to receive the **CloudEvent**s on. CloudEvent messages on this Topic will be processed by a background job.

The background job consists of a custom implementation of the `CloudEventBackgroundJob` which already takes care of topic subscription creation/deletion on start/stop of the job.

## Usage

The mandatory `ProcessMessageAsync` to-be-overriden method allows you to customize the processing of the **CloudEvent** message.

```csharp
public class MyBackgroundJob : CloudEventBackgroundJob
{
	public MyBackgroundJob(
		IConfiguration configuration,
		IServiceProvider serviceProvider,
		ILogger<CloudEventBackgroundJob> logger) : base(configuration, serviceProvider, logger)
	{
		
	}

	protected override async Task ProcessMessageAsync(
		CloudEvent message,
        AzureServiceBusMessageContext messageContext,
        MessageCorrelationInfo correlationInfo,
        CancellationToken cancellationToken)
        {
			// Process the CloudEvent message...
		}
}
```