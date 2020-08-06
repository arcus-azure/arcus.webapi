---
title: "Correlate between HTTP requests/responses via ASP.NET Core middleware"
layout: default
---

# Correlation Between HTTP Requests

The `Arcus.WebApi.Correlation` library provides a way to add correlation between HTTP requests. 

This correlation is based the `RequestId` and `X-Transaction-ID` HTTP request/response headers, however, you can fully configure different headers in case you need to.

## How This Works

When an application is configured to use the default configuration of the correlation, each HTTP response will get an extra header called `RequestId` containing an unique identifier to distinguish between requests/responses.

The `X-Transaction-ID` can be overriden by the request, meaning: if the HTTP request already contains a `X-Transaction-ID` header, the same header+value will be used in the HTTP response.

Additional [configuration](#configuration) is available to tweak this functionality.

## Installation

This feature requires to install our NuGet package:

```shell
PM > Install-Package Arcus.WebApi.Logging
```

## Usage

To make sure the correlation is added to the HTTP response, following additions have to be made in the `.ConfigureServices` and `Configure` methods:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddHttpCorrelation();
}

public void Configure(IApplicationBuilder app, IHostingEnvironment env)
{
    app.UseHttpCorrelation();

    app.UseMvc();
}
```

Note: because the correlation is based on <span>ASP.NET</span> Core middleware, it's recommended to place it before the `.UseMvc` call.

## Configuration

Some extra options are available to alter the functionality of the correlation:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddHttpCorrelation(options =>
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
    });
}
```

## Logging

As an additional feature, we provide an extension to use the HTTP correlation directly in a [Serilog](https://serilog.net/) configuration:

```csharp
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    app.UseHttpCorrelation();
    
    Log.Logger = new LoggerConfiguration()
        .Enrich.WithHttpCorrelationInfo()
        .WriteTo.Console()
        .CreateLogger();
}
```

## Using secret store within Azure Functions

### Installation

For this feature, the following package needs to be installed:

```shell
PM > Install-Package Arcus.WebApi.Logging.AzureFunctions
```

### Usage

To make sure the correlation is added to the HTTP response, following additions have to be made in the `.Configure` methods of the function's startup:

```csharp
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
