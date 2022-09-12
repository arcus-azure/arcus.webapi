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

The HTTP status code `500` is used by default as response code when an unhandled exception is caught. 

### Usage
To use this middleware, add the following line:
```csharp
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