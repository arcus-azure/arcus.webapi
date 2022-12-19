---
title: "Correlate between HTTP requests/responses in Azure Functions"
layout: default
---

# Correlate between HTTP requests/responses in Azure Functions

The `Arcus.WebApi.Logging.AzureFunctions` library provides a way to add correlation between HTTP requests for Azure Functions. 

## How This Works

See [the general HTTP correlation page](correlation.md) to get a grasp on how HTTP correlation works.

🚩 By default, the W3C Trace-Context specification is used as the default HTTP correlation format in Arcus, but you can go back to the (deprecated) Hierarchical system we had before, by passing `HttpCorrelationFormat.Hierarchical` to the `services.AddHttpCorrelation()`.

## Installation

For this feature, the following package needs to be installed:

```shell
PM > Install-Package Arcus.WebApi.Logging.AzureFunctions
```

## Usage for in-process Azure Functions (receiving side)

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
This is how you use the W3C HTTP correlation in your application:

```csharp
using Arcus.WebApi.Logging.Correlation;

public class MyHttpFunction
{
    private readonly AzureFunctionsInProcessHttpCorrelation _httpCorrelation;

    public MyHttpFunction(AzureFunctionsInProcessHttpCorrelation httpCorrelation)
    {
        _httpCorrelation = httpCorrelation;
    }

    [FunctionName("HTTP-Correlation-Example")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
        ILogger log)
    {
        log.LogInformation("C# HTTP trigger function processed a request");

        // Easily set the correlation information to the response if you want to expose it.
        _httpCorrelation.AddCorrelationResponseHeaders(req.HttpContext);

         // Easily access correlation information in your application.
        CorrelationInfo correlationInfo = _httpCorrelation.GetCorrelationInfo();
        return new OkObjectResult("This HTTP triggered function executed successfully.");
    }
}
```

To use the old Hierarchical HTTP correlation, use the following:
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
        log.LogInformation("C# HTTP trigger function processed a request");
        
        if (!_httpCorrelation.TryHttpCorrelate(out string errorMessage))
        {
            return new BadRequestObjectResult(errorMessage);
        }

        // Easily access correlation information in your application.
        CorrelationInfo correlationInfo = _httpCorrelation.GetCorrelationInfo();
        return new OkObjectResult("This HTTP triggered function executed successfully.");
    }
}
```

> Note that the `HttpCorrelation` already has the registered `IHttpCorrelationInfoAccessor` embedded but nothing stops you from injecting the `IHtpCorrelationInfoAccessor` yourself and use that one. Behind the scenes, both instances will be the same.

## Usage for isolated Azure Functions (receiving side)

To make sure the correlation is added to the HTTP response, following middleware has to be added in the `Program.cs` file:
```csharp
using Microsoft.Extensions.Hosting;

IHost host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults(builder =>
    {
        builder.UseFunctionContext()
               .UseHttpCorrelation();
    })
    .Build();

host.Run();
```

* The `UseFunctionContext` middleware makes sure that the `FunctionContext` in the Azure Function is accessible via the `IFunctionContextAccessor` function. This is required since the `IHttpCorrelationInfoAccessor` uses the `FunctionContext` to assign the correlation model.
* The `UseHttpCorrelation` middleware adds the HTTP correlation functionality to the request pipeline. This makes sure that the incoming requests results in a correlation model (accessible via the `IHttpCorrelationInfoAccessor`) and the outgoing response is enriched with this correlation model.

The HTTP trigger function can access the `IHttpCorrelationInfoAccessor` but doesn't require any additional changes to make the HTTP correlation work (unlike the in-process Azure Functions variant).

```csharp
public class HttpTriggerFunction
{
    private readonly IHttpCorrelationInfoAccessor _correlationAccessor;

    public HttpTriggerFunction(IHttpCorrelationInfoAccessor correlationAccessor)
    {
        _correlationAccessor = correlationAccessor;
    }

    [Function("http")]
    public HttpResponseData Run(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData request)
    {
        CorrelationInfo correlationInfo = _correlationAccessor.GetCorrelationInfo();
     
        HttpResponseData response = request.CreateResponse(HttpStatusCode.OK);
        return response;
    }
}
```