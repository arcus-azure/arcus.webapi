---
title: "Logging"
layout: default
---

# Logging

The `Arcus.WebApi.Logging` package provides a way to log several kinds of information during the receival and handling of HTTP requests.

- [Logging unhandled exceptions](#logging-unhandled-exceptions)
- [Logging incoming requests](#logging-incoming-requests)

To send the logging information to Application Insights, see [this explanation](#application-insights).

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

The HTTP request headers are logged by default, except certain security headers are by default omitted: `Authentication`, `X-Api-Key` and `X-ARR-ClientCert`.
The HTTP request body is not logged by default.
See [configuration](#configuration) for more details.

**Example**

`HTTP Request POST http://localhost:5000/weatherforecast completed with 200 in 00:00:00.0191554 at 03/23/2020 10:12:55 +00:00 - (Context: [Content-Type, application/json], [Body, {"today":"cloudy"}])`

**Usage**

To use this middleware, add the following line of code in the `Startup.Configure` method:

```csharp
public void Configure(IApplicationBuilder app, IWebHostEvironment env)
{
    app.UseRequestTracking();

    ...
    app.UseMvc();
}
```

**Configuration**

The request tracking middleware has several configuration options to manipulate what the request logging emits should contain.

```csharp
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    app.UseRequestTracking(options =>
    {
        // Whether or not the HTTP request body should be included in the request tracking logging emits.
        // (default: `false`)
        options.IncludeRequestBody = true;

        // Whether or not the configured HTTP request headers should be included in the request tracking logging emits.
        // (default: `true`)
        options.IncludeRequestHeaders = true;

        // All omitted HTTP request header names that should always be excluded from the request tracking logging emits.
        // (default: `[ "Authentication", "X-Api-Key", "X-ARR-ClientCert" ]`)
        options.OmittedRequestHeaderNames.Add("X-My-Secret-Header");
    });

    ...
    app.UseMvc();
}
```

Optionally, one can inherit from this middleware component and override the default request header sanitization to run some custom functionality during the filtering.

Following example shows how the request security headers can be emptied by not omitted:

```csharp
public class EmptyButNotOmitRequestTrackingMiddleware : RequestTrackingMiddleware
{
    public EmptyButNotOmitRequestTrackingMiddleware(
        RequestDelegate next,
        ILogger<RequestTrackingMiddleware> logger) 
        : base(next, logger)
        {
        }

    protected override IDictionary<string, StringValues> SanitizeRequestHeaders(IDictionary<string, StringValues> requestHeaders)
    {
        requestHeaders["X-Api-Key"] = String.Empty;
        return requestHeaders;
    }
}
```

And, configure your custom middleware like this in the `Startup.cs`:

```csharp
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    app.UseRequestTracking<EmptyButNotOmitRequestTrackingMiddleware>(options => options.OmittedHeaderNames.Clear());

    ...
    app.UseMvc();
}
```

So the resulting log message becomes:

`HTTP Request POST http://localhost:5000/weatherforecast completed with 200 in 00:00:00.0191554 at 03/23/2020 10:12:55 +00:00 - (Context: [X-Api-Key,])`

## Application Insights

To get the information logged in Azure Application Insights, configure logging like this when building the `IWebHost` in `Startup.cs`:

```csharp
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    Log.Logger = new LoggerConfiguration()
        .WriteTo.AzureApplicationInsights("my-instrumentation-key")
        .CreateLogger();
}
```

Note, don't forget to add `.UseSerilog` in the `Program.cs`.

```csharp
WebHost.CreateDefaultBuilder(args)
       .UseApplicationInsights()
       .UseSerilog()
       .UseStartup<Startup>();
```

To have access to the `.AzureApplicationInsights` extension, make sure you've installed [Arcus.Observability.Telemetry.Serilog.Sinks.ApplicationInsights](https://www.nuget.org/packages/Arcus.Observability.Telemetry.Serilog.Sinks.ApplicationInsights/).

[&larr; back](/)
