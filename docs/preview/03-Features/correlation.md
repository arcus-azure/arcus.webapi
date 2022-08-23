---
title: "Correlate between HTTP requests/responses via ASP.NET Core middleware"
layout: default
---

# Correlation Between HTTP Requests

The `Arcus.WebApi.Correlation` library provides a way to add correlation between HTTP requests. 

## How This Works

This diagram shows an example of a user interacting with service A that calls another service B.

![HTTP correlation diagram](/img/http-correlation.png)

Three kind of correlation ID's are used to create the relationship:
* **Transaction ID**: this ID is the one constant in the diagram. This ID is used to describe the entire transaction, from begin to end. All telemetry will be tracked under this ID.
* **Operation ID**: this ID describes a single operation within the transaction. This ID is used within a service to link all telemetry correctly together.
* **Operation Parent ID**: this ID is create the parent/child link across services. When service A calls service B, then service A is the so called 'parent' of service B.

The following list shows each step in the diagram:
1. The initial call in this example doesn't contain any correlation headers. This can be seen as a first interaction call to a service. 
2. Upon receiving at service A, the application will generate new correlation information. This correlation info will be used when telemetry is tracked on the service.
3. When a call is made to service B, the **transaction ID** is sent but also the **operation parent ID** in the form of a hierarchical structure.
4. The `jkl` part of this ID, describes the new parent ID for service B (when service B calls service C, then it will use `jkl` as parent ID)
5. Service B responds to service A with the same information as the call to service B.
6. The user receives both the **transaction ID** and **operation parent ID** in their final response.

💡 This correlation is based on the `RequestId` and `X-Transaction-ID` HTTP request/response headers, however, you can fully configure different headers in case you need to.
💡 The `X-Transaction-ID`  can be overridden by the request, meaning: if the HTTP request already contains a `X-Transaction-ID` header, the same header+value will be used in the HTTP response.

Additional [configuration](#configuration) is available to tweak this functionality.

## Installation

This feature requires to install our NuGet package:

```shell
PM > Install-Package Arcus.WebApi.Logging
```

## Usage

To fully benefit from the Arcus' HTTP correlation functionality, both sending and receiving HTTP endpoints should be configured.

### Sending side

To make sure the correlation is added to the HTTP request, following additions have to be made:

```csharp
using Microsoft.AspNetCore.Builder;

WebApplication builder = WebApplication.CreateBuilder();
builder.Services.AddHttpCorrelation();
builder.Services.AddHttpClient("from-service-a-to-service-b")
                .WithHttpCorrelationTracking();

WebApplication app = builder.Build();
```

Then, the [created `HttpClient`](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests) will be enhanced with HTTP correlation tracking. This means that the request will be altered to include the HTTP correlation information and the endpoint will be tracked as a [HTTP dependency telemetry](https://observability.arcus-azure.net/Features/writing-different-telemetry-types#measuring-http-dependencies).

Alternatively, an existing `HttpClient` can be used to send a correlated HTTP request:

```csharp
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Arcus.WebApi.Logging.Core.Correlation;

var client = new HttpClient();

IHttpCorrelationInfoAccessor accessor = ... // From dependency injection.
ILogger logger = ... // From dependency injection.

var request = new HttpRequestMessage(HttpMethod.Get, "https://localhost/service-b");
await client.SendAsync(request, accessor, logger);
```

💡 The HTTP correlation tracking can also be configured, see [this section](#configuring-http-correlation-client-tracking) for more information.

### Receiving side

To make sure the correlation is added to the HTTP response, following additions have to be made:

```csharp
using Microsoft.AspNetCore.Builder;

WebApplicationBuilder builder = WebApplication.CreateBuilder();
builder.Services.AddHttpCorrelation();

WebApplication app = builder.Build();
app.UseHttpCorrelation();
app.UseRouting();
```

Note: because the correlation is based on <span>ASP.NET</span> Core middleware, it's recommended to place it before the `.UseRouting` call.

## Configuration

The HTTP correlation can be configured with different options to work for your needs.

### Configuring HTTP correlation services

The HTTP correlation is available throughout the application via the registered `IHttpCorrelationInfoAccessor`. This HTTP correlation accessor is both used in sending/receiving functionality.
Some extra options are available to alter the functionality of the correlation:

```csharp
using Microsoft.AspNetCore.Builder;

WebApplicationBuilder builder = WebApplication.CreateBuilder();
builder.Services.AddHttpCorrelation(options =>
{
    // Configuration on the transaction ID (`X-Transaction-ID`) request/response header.
    // ---------------------------------------------------------------------------------

    // Whether the transaction ID can be specified in the request, and will be used throughout the request handling.
    // The request will return early when the `.AllowInRequest` is set to `false` and the request does contain the header (default: true).
    options.Transaction.AllowInRequest = true;

    // Whether or not the transaction ID should be generated when there isn't any transaction ID found in the request.
    // When the `.GenerateWhenNotSpecified` is set to `false` and the request doesn't contain the header, no value will be available for the transaction ID; 
    // otherwise a GUID will be generated (default: true).
    options.Transaction.GenerateWhenNotSpecified = true;

    // Whether to include the transaction ID in the response (default: true).
    options.Transaction.IncludeInResponse = true;

    // The header to look for in the HTTP request, and will be set in the HTTP response (default: X-Transaction-ID).
    options.Transaction.HeaderName = "X-Transaction-ID";

    // The function that will generate the transaction ID, when the `.GenerateWhenNotSpecified` is set to `false` and the request doesn't contain the header.
    // (default: new `Guid`).
    options.Transaction.GenerateId = () => $"Transaction-{Guid.NewGuid()}";

    // Configuration on the operation ID (`RequestId`) response header.
    // ----------------------------------------------------------------

    // Whether to include the operation ID in the response (default: true).
    options.Operation.IncludeInResponse = true;

    // The header that will contain the operation ID in the HTTP response (default: RequestId).
    options.Operation.HeaderName = "RequestId";

    // The function that will generate the operation ID header value.
    // (default: new `Guid`).
    options.Operation.GenerateId = () => $"Operation-{Guid.NewGuid()}";

    // Configuration on operation parent ID request header (`Request-Id`).
    // ------------------------------------------------------------------

    // Whether to extract the operation parent ID from the incoming request following W3C Trace-Context standard (default: true).
    // More information on operation ID and operation parent ID, see [this documentation](https://docs.microsoft.com/en-us/azure/azure-monitor/app/correlation).
    options.UpstreamService.ExtractFromRequest = false;

    // The header that will contain the operation parent ID in the HTTP request (default: Request-Id).
    options.UpstreamService.HeaderName = "x-request-id";

    // The function that will generate the operation parent ID when it shouldn't be extracted from the request.
    options.UpstreamService.GenerateId = () => $"Parent-{Guid.newGuid()}";
});
```

### Configuring HTTP correlation client tracking

When sending tracked HTTP requests, some options can be configured to customize the tracking to your needs.

```csharp
using Microsoft.AspNetCore.Builder;

WebApplicationBuilder builder = WebApplication.CreateBuilder();
builder.Services.AddHttpClient("from-service-a-to-service-b")
                .WithHttpCorrelationTracking(options =>
                {
                    // The header that will be used to set the HTTP correlation transaction ID. (Default: X-Transaction-ID)
                    options.TransactionIdHeaderName = "X-MyTransaction-Id";

                    // The header that will be used to set the upstream service correlation ID. (Default: Request-Id)
                    options.UpstreamServiceHeaderName = "X-MyRequest-Id";

                    // The function to generate the dependency ID for the called service. 
                    // (service A tracks dependency with this ID, service B tracks request with this ID).
                    options.GenerateDependencyId = () => $"my-request-id-{Guid.NewGuid()}";

                    // The dictionary containing any additional contextual inforamtion that will be used when tracking the HTTP dependency (Default: empty dictionary).
                    options.AddTelemetryContext(new Dictionary<string, object>
                    {
                        ["My-HTTP-custom-key"] = "Any additional information"
                    });
                });
```

The same options can be configured when sending correlated HTTP requests with an existing `HttpClient`:

```csharp
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Arcus.WebApi.Logging.Core.Correlation;

var client = new HttpClient();

IHttpCorrelationInfoAccessor accessor = ... // From dependency injection.
ILogger logger = ... // From dependency injection.

var request = new HttpRequestMessage(HttpMethod.Get, "https://localhost/service-b");
await client.SendAsync(request, accessor, logger, options =>
{
    // The header that will be used to set the HTTP correlation transaction ID. (Default: X-Transaction-ID)
    options.TransactionIdHeaderName = "X-MyTransaction-Id";

    // The header that will be used to set the upstream service correlation ID. (Default: Request-Id)
    options.UpstreamServiceHeaderName = "X-MyRequest-Id";

    // The function to generate the dependency ID for the called service. 
    // (service A tracks dependency with this ID, service B tracks request with this ID).
    options.GenerateDependencyId = () => $"my-request-id-{Guid.NewGuid()}";
});
```

## Dependency injection

To use the HTTP correlation in your application code, you can use a dedicated marker interface called `IHttpCorrelationInfoAccessor`.
This will help you with accessing and setting the HTTP correlation.

Note that the correlation is a scoped dependency, so will be the same instance across the HTTP request.

```csharp
using Microsoft.AspNetCore.Mvc;
using Arcus.WebApi.Logging.Core.Correlation;

[ApiController]
[Route("api/v1/order")]
public class OrderController : ControllerBase
{
    private readonly IHttpCorrelationInfoAccessor _accessor;

    public OrderController(IHttpCorrelationInfoAccessor accessor)
    {
        _accessor = accessor;
    }

    [HttpPost]
    public IActionResult Post([FromBody] Order order)
    {
        CorrelationInfo correlation = _accessor.GetCorrelationInfo();

        _accessor.SetCorrelationInfo(correlation);
    }
}
```

## Logging

As an additional feature, we provide an extension to use the HTTP correlation directly in a [Serilog](https://serilog.net/) configuration as an [enricher](https://github.com/serilog/serilog/wiki/Enrichment). 
This adds the correlation information of the current request to the log event as a log property called `TransactionId`, `OperationId`, and `OperationParentId`.

**Example**

- `TransactionId`: `A5E90591-ADB0-4A56-818A-AC5C02FBFF5F`
- `OperationId`: `79BB196A-B0CC-4F5C-B48A-AB87850346AF`
- `OperationParentId`: `0BC101AC-5E41-43B5-B020-3EF467629E3D`

**Usage**
The enricher requires access to the application services so it can get the correlation information.

```csharp
using Microsoft.AspNetCore.Builder;
using Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder();
builder.Host.UseSerilog((context, serviceProvider, config) =>
{
    return new LoggerConfiguration()
        .Enrich.WithHttpCorrelationInfo(serviceProvider)
        .WriteTo.Console()
        .CreateLogger();
});

WebApplication app = builder.Build();
app.UseHttpCorrelation();
```

## Using correlation within Azure Functions

### Installation

For this feature, the following package needs to be installed:

```shell
PM > Install-Package Arcus.WebApi.Logging.AzureFunctions
```

### Usage

To make sure the correlation is added to the HTTP response, following additions have to be made in the `.Configure` methods of the function's startup:

```csharp
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Startup))]

namespace MyHttpAzureFunction
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.AddHttpCorrelation();
        }
    }
}
```

When this addition is added, you can use the `HttpCorrelation` inside your function to call the correlation functionality and use the `IHttpCorrelationInfoAccessor` (like before) to have access to the `CorrelationInfo` of the HTTP request.

```csharp
using Arcus.WebApi.Logging.Correlation;

public class MyHttpFunction
{
    private readonly HttpCorrelation _httpCorrelation;

    public MyHttpFunction(HttpCorrelation httpCorrelation)
    {
        _httpCorrelation = httpCorrelation;
    }

    [FunctionName("HTTP-Correlation-Example")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
        ILogger log)
    {
        if (_httpCorrelation.TryHttpCorrelate(out string errorMessage))
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            // Easily access correlation information in your application
            CorrelationInfo correlationInfo = _httpCorrelation.GetCorrelationInfo();
            return new OkObjectResult("This HTTP triggered function executed successfully.");
        }

        return new BadRequestObjectResult(errorMessage);
    }
```

> Note that the `HttpCorrelation` already has the registered `IHttpCorrelationInfoAccessor` embedded but nothing stops you from injecting the `IHtpCorrelationInfoAccessor` yourself and use that one. Behind the scenes, both instances will be the same.
