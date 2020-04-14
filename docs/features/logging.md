---
title: "Logging"
layout: default
---

# Logging

The `Arcus.WebApi.Logging` package provides a way to log several kinds of information during the receival and handling of HTTP requests.

- [Logging unhandled exceptions](#logging-unhandled-exceptions)
- [Logging incoming requests](#logging-incoming-requests)

To send the logging information to Application Insights, see [this explenation](#application-insights).

## Installation

These features requires to install our NuGet package

```shell
PM > Install-Package Arcus.WebApi.Logging
```

## Logging unhandled exceptions

The `ExceptionHandlingMiddleware` class can be added to the <span>ASP.NET</span> Core pipeline to log unhandled exceptions that are thrown during request processing.
The unhandled exceptions are caught by this middleware component and are logged through the `ILogger` implementations that are configured inside the project.

The HTTP status code `500` is used as response code when an unhandled exception is caught. 
However, when the runtime throws a `BadHttpRequestException` we will reflect this by returning the corresponding status code determined by the runtime.

**Usage**

To use this middleware, add the following line of code in the `Startup.Configure` method:

```csharp
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
   app.UseMiddleware<Arcus.WebApi.Logging.ExceptionHandlingMiddleware>();

   ...
   app.UseMvc();
}
```

If you have multiple middleware components configured, it is advised to put the `ExceptionHandlingMiddleware` as the first one.
By doing so, unhandled exceptions that might occur in other middleware components will also be logged by the `ExceptionHandlingMiddleware` component.

## Logging incoming requests

The `RequestTrackingMiddleware` class can be added to the <span>ASP.NET</span> Core pipeline to log all received HTTP requests.
The requests by this middleware component are logged through the `ILogger` implementations that are configured inside the project.

The HTTP request headers and request body are by default logged.

**Example**

`HTTP Request POST http://localhost:5000/weatherforecast completed with 200 in 00:00:00.0191554 at 03/23/2020 10:12:55 +00:00 - (Context: [Content-Type, application/json], [Body, {"today":"cloudy"}])`

**Usage**

To use this middleware, add the following line of code in the `Startup.Configure` method:

```csharp
public void Configure(IApplicationBuilder app, IWebHostEvironment env)
{
    app.UseMiddleware<Arcus.WebApi.Logging.RequestTrackingMiddleware>();

    ...
    app.UseMvc();
}
```

**Configuration**

Optionally, one can inherit from this middleware component and override the default request header and body extraction to discared one or both.
Following example discards the request headers:

```csharp
public class NoRequestHeadersRequestTrackingMiddleware : RequestTrackingMiddleware
{
    public NoRequestHeadersRequestTrackingMiddleware(
        RequestDelegate next,
        ILogger<RequestTrackingMiddleware> logger) 
        : base(next, logger)
        {
        }

    protected override IDictionary<string, object> ExtractRequestHeaders(IHeaderDictionary requestHeaders)
    {
        return new Dictionary<string, object>();
    }
}
```

So the resulting log message becomes:

`HTTP Request POST http://localhost:5000/weatherforecast completed with 200 in 00:00:00.0191554 at 03/23/2020 10:12:55 +00:00 - (Context: [Content-Type, application/json])`

## Application Insights

To get the information logged in Azure Application Insights, configure logging like this when building the `IWebHost` in `Program.cs`:

```csharp
WebHost.CreateDefaultBuilder(args)
       .UseApplicationInsights()
       .ConfigureLogging(loggers => loggers.AddApplicationInsights())
       .UseStartup<Startup>();
```

To be able to use the `AddApplicationInsights` extension method, the Microsoft.Extensions.Logging.ApplicationInsights package must be installed.

If the parameter-less `AddApplicationInsights` method is used, the configurationsetting `ApplicationInsights:InstrumentationKey` must be specified and the value of the instrumentation-key of the Application Insights resource must be set.


[&larr; back](/)
