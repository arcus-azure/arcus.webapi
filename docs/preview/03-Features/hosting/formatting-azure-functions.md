---
title: "JSON formatting for Azure Functions (isolated)"
layout: default
---

# JSON formatting
The `Arcus.WebApi.Hosting.AzureFunctions` library provides a way to restrict and configure the JSON input and output formatting of your application.
This allows for a easier and more secure formatting when working with JSON types.

> ⚠ These features are only available for Azure Functions using the isolated functions worker. For more information on the difference between in-process and isolated Azure Functions, see [Microsoft's documentation](https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide).

## Installation
These features require to install our NuGet package:

```shell
PM > Install-Package Arcus.WebApi.Hosting.AzureFunctions
```

## Restricting JSON format
We have provided an extension that will allow you to restrict your input and output formatting to only JSON formatting (Only the `SystemTextJsonInputFormatter` will remain). 
This means that all other incoming content will result in `UnsupportedMediaType` failures and outgoing content will fail to serialize back to the sender. With this functionality, you'll be sure that you only have to deal with JSON.

❗ Make sure that the `Content-Type` and `Allow` are set to `application/json`.

Following example shows you how you can configure this:

```csharp
using Microsoft.Extensions.Hosting;

 IHost host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults(builder =>
    {
        builder.UseOnlyJsonFormatting();
    })
    .Build();
```

## Configure JSON format
We have provided an extension that will allow you to configure a [`JsonObjectSerializer`](https://learn.microsoft.com/en-us/dotnet/api/azure.core.serialization.jsonobjectserializer?view=azure-dotnet) which will be registered in the application services. This JSON serializer can be injected in your Azure Function implementation so that incoming and outgoing models are handled the same way, across multiple functions.
This makes the JSON formatting more centralized and easier to maintain.

Following example shows you how you can configure these options:

```csharp
using System.Text.Json;
using Microsoft.Extensions.Hosting;

IHost host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults(builder =>
    {
        builder.ConfigureJsonFormatting(options =>
        {
            options.Converters.Add(new JsonStringEnumConverter());
        });
    })
    .Build();
```

After that, the `JsonObjectSerializer` can be injected in your function for deserialization/serialization of incoming/outgoing models:

```csharp
using Azure.Core.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

public class OrderFunction
{
    private readonly JsonObjectSerializer _jsonSerializer;

    public OrderFunction(JsonObjectSerializer jsonSerializer)
    {
        _jsonSerializer = jsonSerializer;
    }

    [Function("order-processing")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData request)
    {
        var order = await request.ReadFromJsonAsync<Order>(_jsonSerializer);

        HttpResponseData response = request.CreateResponse();
        await response.WriteAsJsonAsync(order, _jsonSerializer);
        return response;
    }
}
```