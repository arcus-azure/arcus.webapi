---
title: "Correlate between HTTP requests/responses via ASP.NET Core middleware"
layout: default
---

# Correlation Between HTTP Requests

The `Arcus.WebApi.Correlation` library provides a way to add correlation between HTTP requests. 

This correlation is based on the `RequestId` and `X-Transaction-ID` HTTP request/response headers, however, you can fully configure different headers in case you need to.

- [Correlation Between HTTP Requests](#correlation-between-http-requests)
  - [How This Works](#how-this-works)
  - [Installation](#installation)
  - [Usage](#usage)
  - [Configuration](#configuration)
  - [Dependency injection](#dependency-injection)
  - [Logging](#logging)
  - [Using correlation within Azure Functions](#using-correlation-within-azure-functions)
    - [Installation](#installation-1)
    - [Usage](#usage-1)

## How This Works

When an application is configured to use the default configuration of the correlation, each HTTP **response** will have an extra header called `RequestId` containing an unique identifier to distinguish between requests/responses. This ID will act as the parent ID for all telemetry that comes after and uses the [HTTP Correlation](https://github.com/dotnet/runtime/blob/main/src/libraries/System.Diagnostics.DiagnosticSource/src/HttpCorrelationProtocol.md) to extract the most recent ID.

The `X-Transaction-ID` can be overridden by the request, meaning: if the HTTP request already contains a `X-Transaction-ID` header, the same header+value will be used in the HTTP response.

Additional [configuration](#configuration) is available to tweak this functionality.

## Installation

This feature requires to install our NuGet package:

```shell
PM > Install-Package Arcus.WebApi.Logging
```

## Usage

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
    options.OperationParent.ExtractFromRequest = false;

    // The header that will contain the operation parent ID in the HTTP request (default: Request-Id).
    options.OperationParent.OperationParentIdHeaderName = "x-request-id";

    // The function that will generate the operation parent ID when it shouldn't be extracted from the request.
    options.OperationParent.GenerateId = () => $"Parent-{Guid.newGuid()}";
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
This adds the correlation information of the current request to the log event as a log property called `TransactionId` and `OperationId`.

**Example**

- `TransactionId`: `A5E90591-ADB0-4A56-818A-AC5C02FBFF5F`
- `OperationId`: `79BB196A-B0CC-4F5C-B48A-AB87850346AF`

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

When this addition is added, you can use the `HttpCorrelation` inside your function to call the correlation functionality and use the `ICorrelationInfoAccessor` (like before) to have access to the `CorrelationInfo` of the HTTP request.

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

> Note that the `HttpCorrelation` already has the registered `ICorrelationInfoAccessor` embedded but nothing stops you from injecting the `ICorrelationInfoAccessor` yourself and use that one. Behind the scenes, both instances will be the same.

[&larr; back](/)
