---
title: "Logging in Azure Functions (isolated)"
layout: default
---

# Logging in Azure Functions (isolated)
The `Arcus.WebApi.Logging.AzureFunctions` package provides a way to log several kinds of information during the receival and handling of HTTP requests.

To send the logging information to Application Insights, see [this user guide](https://observability.arcus-azure.net/Guidance/use-with-dotnet-and-functions).

## Installation
These features require to install our NuGet package

```shell
PM > Install-Package Arcus.WebApi.Logging.AzureFunctions
```

## Logging unhandled exceptions
The `AzureFunctionsExceptionHandlingMiddleware` class can be added to the Azure Functions worker pipeline to log unhandled exceptions that are thrown during request processing.
The unhandled exceptions are caught by this middleware component and are logged through the `ILogger` implementations that are configured inside the project.

> ⚡ The HTTP status code `500` is used by default as response code when an unhandled exception is caught. 

### Usage
To use this middleware, add the following line:
```csharp
using Microsoft.Extensions.Hosting;

public class Program
{
    public static void Main(string[] args)
    {
        IHost host = new HostBuilder()
            .ConfigureFunctionsWorkerDefaults(builder =>
            {
                builder.UseExceptionHandling();
            })
            .Build();
    
        host.Run();
    }
}
```

### Configuration
When custom exception handling is required, you can inherit from the `AzureFunctionsExceptionHandlingMiddleware` to create your own middleware component and register it with Arcus' extension.

This example implements the exception handling middleware to influence the log message and adds a custom determination of the HTTP response status code.
```csharp
using Arcus.WebApi.Logging.AzureFunctions;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

public class MyExceptionHandlingMiddleware : AzureFunctionsExceptionHandlingMiddleware
{
    protected override void LogException(ILogger logger, Exception exception)
    {
        logger.LogCritical(exception, "Custom exception handling!");
    }

    protected override HttpResponseData CreateFailureResponse(Exception exception, HttpStatusCode defaultFailureStatusCode, HttpRequestData request)
    {
        if (exception is ValidationException)
        {
            return request.CreateResponse(HttpStatusCode.BadRequest);
        }
        else if (exception is TimeoutException)
        {
            return request.CreateResponse(HttpStatusCode.ServerTimeout);
        }
        else 
        {
            return request.CreateResponse(defaultFailureStatusCode);
        }
    }
}
```

Next, make sure that you pass along the exception middleware to the exception handling extension.
```csharp
.ConfigureFunctionsWorkerDefaults(builder =>
{
    app.UseExceptionHandling<MyExceptionHandlingMiddleware>();
});
```

## Logging incoming requests
The `AzureFunctionsRequestTrackingMiddleware` class can be added to the Azure Functions worker pipeline to log any incoming HTTP requests.
The requests are tracked with [Arcus Observability](https://observability.arcus-azure.net/Features/writing-different-telemetry-types#incoming-http-requests-in-azure-function-http-trigger) so that they will show up as requests in Application Insights when the application is using [Arcus Application Insights Serilog sink](https://observability.arcus-azure.net/Features/sinks/azure-application-insights).

> ⚠ The HTTP request and response body are not tracked by default.

> ⚡ The HTTP request headers are logged by default, except certain security headers are by default omitted: `Authentication`, `X-Api-Key` and `X-ARR-ClientCert`.

Tracking a HTTP request will look like this in the logs:
`HTTP Request POST http://localhost:5000/weatherforecast completed with 200 in 00:00:00.0191554 at 03/23/2020 10:12:55 +00:00 - (Context: [Content-Type, application/json], [Body, {"today":"cloudy"}])`

### Usage
To use this middleware component, add the following line to your startup code:
```csharp
using Microsoft.Extensions.Hosting;

public class Program
{
    public static void Main(string[] args)
    {
        IHost host = new HostBuilder()
            .ConfigureFunctionsWorkerDefaults(builder =>
            {
                builder.UseRequestTracking();
            })
            .Build();
    
        host.Run();
    }
}
```

### Configuration
The middleware component can be configured to influence the behavior of the HTTP request tracking. This ranges from including HTTP request/response bodies, specifying which routes should be tracked, filtering HTTP request headers, and more.
To learn more about these options, see [the configuration section at the general Web API page](./logging.md) as these options are identical for Azure Functions HTTP triggers and Web API's.

### Customization
Optionally, the middleware component can be extended even further by inheriting from the `AzureFunctionsRequestTrackingMiddleware` class. This allows full control over the sanitation process of the HTTP request/response body and HTTP request headers.

The following example shows how a custom implementation makes sure that a specific header is not entirely excluded but is redacted from the HTTP request tracking.
```csharp
public class RedactedRequestTrackingMiddleware : AzureFunctionsRequestTrackingMiddleware
{
    public RedactedRequestTrackingMiddleware(RequestTrackingOptions options) : base(options)
    {
    }

    protected override IDictionary<string, StringValues> SanitizeRequestHeaders(IDictionary<string, StringValues> requestHeaders)
    {
        var  headerName = "X-Private-Client-Id";
        if (requestHeaders.TryGetValue(headerName, out StringValues value))
        {
            requestHeaders[headerName] = "<redacted>";
        }
    }
}
```

> 💡 Note that the custom middleware also has an constructor overload to pass-in additional options so you can benefit also from custom defined options that alter the behavior of your custom sanitization process in your custom middleware component.

This custom middleware component can be registered with an `.UseRequestTracking<>()` extension overload, which allows you to configure any additional options, if required.
```csharp
using Microsoft.Extensions.Hosting;

public class Program
{
    public static void Main(string[] args)
    {
        IHost host = new HostBuilder()
            .ConfigureFunctionsWorkerDefaults(builder =>
            {
                builder.UseRequestTracking<RedactedRequestTrackingMiddleware>();
            })
            .Build();
    
        host.Run();
    }
}
```