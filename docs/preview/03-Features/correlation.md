---
title: "Correlate between HTTP requests/responses (W3C) via ASP.NET Core middleware"
layout: default
---

# Correlation Between HTTP Requests (W3C)
The `Arcus.WebApi.Logging` library provides a way to add correlation between HTTP requests. 

🚩 This page describes W3C HTTP correlation, see [this page](./correlation-hierarchical.md) for information on the (deprecated) Hierarchical HTTP correlation.

## How This Works
This diagram shows an example of a user interacting with service A that calls another service B.

![HTTP correlation diagram](/img/http-correlation-w3c.png)

Three kind of correlation ID's are used to create the relationship:
* **Transaction ID**: this ID is the one constant in the diagram. This ID is used to describe the entire transaction, from begin to end. All telemetry will be tracked under this ID.
* **Operation ID**: this ID describes a single operation within the transaction. This ID is used within a service to link all telemetry correctly together.
* **Operation Parent ID**: this ID is create the parent/child link across services. When service A calls service B, then service A is the so called 'parent' of service B.

The following list shows each step in the diagram:
1. The initial call in this example doesn't contain any correlation headers. This can be seen as a first interaction call to a service. 
2. Upon receiving at service A, the application will generate new correlation information. This correlation info will be used when telemetry is tracked on the service.
3. When a call is made to service B, the **transaction ID** is sent but also the **operation parent ID** in the form of a W3C structure: `00-transactionId-parentId-00`.
4. The `def` part of this ID, describes the new parent ID for service B (when service B calls service C, then it will use a different parent ID)
5. Service B responds to service A with the same information as the call to service B.
6. The user receives both the **transaction ID** and **operation ID** in their final response.

💡 This correlation is based on the `traceparent` HTTP request/response header, however.

Additional [configuration](#configuration) is available to tweak this functionality.

### ⚡ Automatic dependency tracking
When choosing for the W3C HTTP correlation format, Arcus and Microsoft technology works seemingly together. When a HTTP request is received on a service that uses the Arcus W3C HTTP correlation, all remote dependencies managed my Microsoft (HTTP, ServiceBus, EventHubs...) are tracked automatically, without additional configuration.

## Installation
This feature requires to install our NuGet package:

```shell
PM > Install-Package Arcus.WebApi.Logging
```

## Usage
To fully benefit from the Arcus' HTTP correlation functionality, both sending and receiving HTTP endpoints should be configured.

### Sending side
To make sure the correlation is added to the HTTP request, following additions have to be made.

```csharp
using Microsoft.AspNetCore.Builder;

WebApplication builder = WebApplication.CreateBuilder();
builder.Services.AddHttpCorrelation();
builder.Services.AddHttpClient("from-service-a-to-service-b");

WebApplication app = builder.Build();
```

### Receiving side
To make sure the correlation is added to the HTTP response, following additions have to be made:

```csharp
using Microsoft.AspNetCore.Builder;

WebApplicationBuilder builder = WebApplication.CreateBuilder();
builder.Services.AddHttpCorrelation();

WebApplication app = builder.Build();
app.UseHttpCorrelation();
app.UseRouting();
app.UseRequestTracking();
```

> ⚠ Because the correlation is based on <span>ASP.NET</span> Core middleware, it's recommended to place it before the `.UseRouting` call.

> ⚡ To use HTTP correlation in Azure Functions, see [this dedicated page](correlation-azure-functions.md), as the configuration on the receiving is slightly different.

The `UseRequestTracking` extension will make sure that the incoming HTTP request will be tracked as a 'request' in Application Insights (if configured).
For more information on HTTP request tracking, see [our dedicated feature documentation page](./logging.md);

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
    // Configuration on the transaction ID (`X-Transaction-ID`) response header.
    // ---------------------------------------------------------------------------------

    // Whether to include the transaction ID in the response (default: true).
    options.Transaction.IncludeInResponse = true;

    // The header will be set in the HTTP response (default: X-Transaction-ID).
    options.Transaction.HeaderName = "X-Transaction-ID";

    // Configuration on the operation ID (`X-Operation-Id`) response header.
    // ----------------------------------------------------------------

    // Whether to include the operation ID in the response (default: true).
    options.Operation.IncludeInResponse = true;

    // The header that will contain the operation ID in the HTTP response (default: X-Operation-Id).
    options.Operation.HeaderName = "X-MyOperation-Id";

    // Configuration on operation parent ID request header (`traceparent`).
    // ------------------------------------------------------------------

    // The header that will contain the operation parent ID in the HTTP request (default: traceparent).
    options.UpstreamService.HeaderName = "x-request-id";
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

- `TransactionId`: `4b1c0c8d608f57db7bd0b13c88ef865e`
- `OperationId`: `4a3c1c8d`
- `OperationParentId`: `4c6893cc6c6cad10`

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
app.UseRouting();
app.UseRequestTracking();
```
