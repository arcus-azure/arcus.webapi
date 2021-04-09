---
title: "Logging"
layout: default
---

# Logging

The `Arcus.WebApi.Logging` package provides a way to log several kinds of information during the receival and handling of HTTP requests.

- [Logging unhandled exceptions](#logging-unhandled-exceptions)
- [Logging incoming requests](#logging-incoming-requests)
- [Tracking application version](#tracking-application-version)

To send the logging information to Application Insights, see [this explanation](#application-insights).

## Installation

These features require to install our NuGet package

```shell
PM > Install-Package Arcus.WebApi.Logging
```

## Logging unhandled exceptions

The `ExceptionHandlingMiddleware` class can be added to the <span>ASP.NET</span> Core pipeline to log unhandled exceptions that are thrown during request processing.
The unhandled exceptions are caught by this middleware component and are logged through the `ILogger` implementations that are configured inside the project.

The HTTP status code `500` is used as response code when an unhandled exception is caught. 
However, when the runtime throws a `BadHttpRequestException` we will reflect this by returning the corresponding status code determined by the runtime.

### Usage

To use this middleware, add the following line of code in the `Startup.Configure` method:

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

public class Startup
{
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
       app.UseExceptionHandling();

       ...
       app.UseMvc();
    }
}
```

If you have multiple middleware components configured, it is advised to put the `ExceptionHandlingMiddleware` as the first one.
By doing so, unhandled exceptions that might occur in other middleware components will also be logged by the `ExceptionHandlingMiddleware` component.

## Logging incoming requests

The `RequestTrackingMiddleware` class can be added to the <span>ASP.NET</span> Core pipeline to log all received HTTP requests.
The incoming requests are logged by this middleware component using the `ILogger` implementations that are configured in the project.

The HTTP request body is not logged by default.

The HTTP request headers are logged by default, except certain security headers are by default omitted: `Authentication`, `X-Api-Key` and `X-ARR-ClientCert`.
See [configuration](#configuration) for more details.

### Example

`HTTP Request POST http://localhost:5000/weatherforecast completed with 200 in 00:00:00.0191554 at 03/23/2020 10:12:55 +00:00 - (Context: [Content-Type, application/json], [Body, {"today":"cloudy"}])`

### Usage

To use this middleware, add the following line of code in the `Startup.Configure` method:

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

public class Startup
{
    public void Configure(IApplicationBuilder app, IWebHostEvironment env)
    {
         // In versions less than .NET Core 3.0:
        // make sure that the endpoint routing is specified before the `UseRequestRouting` if you want to make use of the `SkipRequestTrackingAttribute`.
        app.UseEndpointRouting();

        // In versions starting from .NET Core 3.0:
        // make sure that the endpoint routing is specified before the `UseRequestRouting` if you want to make use of the `SkipRequestTrackingAttribute`.
        app.UseRouting();

        app.UseRequestTracking();

        ...
        app.UseMvc();
    }
}
```

### Configuration

The request tracking middleware has several configuration options to manipulate what the request logging emits should contain.

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

public class Startup
{
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        // In versions less than .NET Core 3.0:
        // make sure that the endpoint routing is specified before the `UseRequestRouting` if you want to make use of the `SkipRequestTrackingAttribute`.
        app.UseEndpointRouting();

        // In versions starting from .NET Core 3.0:
        // make sure that the endpoint routing is specified before the `UseRequestRouting` if you want to make use of the `SkipRequestTrackingAttribute`.
        app.UseRouting();

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

            // Size of the request body buffer (in bytes) which should be tracked. Request bodies greater than this buffer will only be partly present in the telemetry.
            // (default: no limit)
            options.RequestBodyBufferSize = 10 * 1024 * 1024; // 10MB

            // Whether or not the HTTP response body should be included in the request tracking logging emits.
            // (default: `false`)
            options.IncludeResponseBody = true;

            // Size of the response body buffer (in bytes) hwhich should be tracked. Response bodies greater than this buffer will only be partly present in the telemetry.
            // (default: no limit)
            options.ResponseBodyBufferSize = 10 * 1024 * 1024; // 10MB
        });

        ...

        app.UseMvc();
    }
}
```

### Excluding certain routes

You can opt-out for request tracking on one or more endpoints (operation and/or controller level).
This can come in handy if you have certain endpoints with rather large request bodies or want to boost performance or are constantly probed to monitor the application.
This can easily be done by using the `ExcludeRequestTrackingAttribute` on the endpoint or controller of your choosing.

```csharp
[ApiController]
[ExcludeRequestTracking]
public class OrderController : ControllerBase
{
    [HttpPost]
    public IActionResult BigPost()
    {
        Stream bigRequestBody = Request.Body;
        return Ok();
    }
}
```

#### Excluding request/response bodies

When used as in the example above, then all routes of the controller will be excluded from the telemetry tracking. 
The exclude attribute allows you to specify on a more specific level what part of the request should be excluded.

```csharp
[ApiController]
public class OrderController : ControllerBase
{
    // Only exclude the request body from the request tracking. 
    // The request will still be tracked and will contain the request headers and the response body (if configured).
    [HttpPost]
    [RequestTracking(Exclude.RequestBody)]
    public IActionResult BigRequestBodyPost()
    {
        Stream bigRequestBody = Request.Body;
        return Ok();
    }

    // Only exclude the response body from the request tracking.
    // The request will still be tracked and will contain the request headers and the request body (if configured).
    [HttPost]
    [RequestTracking(Exclude.ResponseBody)]
    public async Task<IActionResult> BigResponseBodyPost()
    {
        Stream responseBody = ...
        responseBody.CopyToAsync(Response.Body);

        return Ok();
    }
}
```

### Customization

Optionally, one can inherit from this middleware component and override the default request header sanitization to run some custom functionality during the filtering.

Following example shows how the request security headers can be emptied by not omitted:

```csharp
using Arcus.WebApi.Logging;

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
        requestHeaders["X-Api-Key"] = "<redacted>";
        return requestHeaders;
    }
}
```

And, configure your custom middleware like this in the `Startup.cs`:

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

public class Startup
{
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
         // In versions less than .NET Core 3.0:
        // make sure that the endpoint routing is specified before the `UseRequestRouting` if you want to make use of the `SkipRequestTrackingAttribute`.
        app.UseEndpointRouting();

        // In versions starting from .NET Core 3.0:
        // make sure that the endpoint routing is specified before the `UseRequestRouting` if you want to make use of the `SkipRequestTrackingAttribute`.
        app.UseRouting();

        app.UseRequestTracking<EmptyButNotOmitRequestTrackingMiddleware>(options => options.OmittedHeaderNames.Clear());

        ...
        app.UseMvc();
    }
}
```

So the resulting log message becomes:

`HTTP Request POST http://localhost:5000/weatherforecast completed with 200 in 00:00:00.0191554 at 03/23/2020 10:12:55 +00:00 - (Context: [X-Api-Key,])`

#### Tracked HTTP status codes

By default, all the responded HTTP status codes will be tracked but this can be altered according to your chosing.

Consider the following API controller. When we configure request tracking, both the `400 BadRequest` response as the `201 Created` response will be tracked.

```csharp
[ApiController]
public class OrderController : ControllerBase
{
    [HttpPost]
    public IActionResult Create([FromBody] Order order)
    {
        if (order.Id is null)
        {
            // Request tracking will happen on this response.
            return BadRequest("Order ID should be filled out");
        }

        // Request tracking will happend on this response.
        return Created($"/orders/{order.Id}", order);
    }
}
```

Let's change the request tracking to only track successful '201 Created' responses.
This can be changed via the options:

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

public class Startup
{
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseRequestTracking(options => options.TrackedStatusCodes.Add(HttpStatusCode.Created));
    }
}
```

This means that every endpoint will only track `201 Created` responses. Changing this in the options usually for when you want to straightline your entire application and only allow a certain set of status codes to be tracked.
More grainer control can be achieved via placing an attribute on either the controller's class definition or the endpoint method:

```csharp
using Arcus.WebApi.Logging;

[ApiController]
public class OrderController : ControllerBase
{
    [HttpPost]
    [RequestTracking(HttpStatusCode.Created)]
    public IActionResult Create([FromBody] Order order)
    {
        if (order.Id is null)
        {
            // No request tracking will apear in the logs.
            return BadRequest("Order ID should be filled out");
        }

        // Request tracking will only happen on this response.
        return Created($"/orders/{order.Id}", order);
    }
}
```

> Note that this `RequestTracking` attribute can be added multiple times and works together with the configured options. 
> The end result will be the accumulated result of all the applied attributes, both on the method and on the controller, and the configured options.

## Tracking application version

The `Arcus.WebApi.Logging` library allows you to add application version tracking to your <span>ASP.NET</span> application which will include your application version to a configurable response header.

This functionality uses the `IAppVersion`, available in the [Arcus.Observability](https://observability.arcus-azure.net/features/telemetry-enrichment#version-enricher) library, for retrieving the current application version.
Such an instance **must** be registered in order for the version tracking to work correctly.

> ⚠ **Warning:** Only use the version tracking for non-public endpoints otherwise the version information is leaked and it can be used for unintended malicious purposes.

Adding the version tracking can be done by the following:

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IAppVersion, MyAppVersion>();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        // Uses the previously registered `IAppVersion` service to include the application to the default `X-Version` response header.
        app.UseVersionTracking();

        // Uses the previously registered `IAppVersion` service to include the application to the custom `X-My-Version` response header.
        app.UseVersionTracking(options => options.Header = "X-My-Version");
    }
}
```

## Application Insights

To get the information logged in Azure Application Insights, configure logging like this when building the `IWebHost` in `Startup.cs`:

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Serilog;
using Serilog.Configuration;

public class Startup
{
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.AzureApplicationInsights("my-instrumentation-key")
            .CreateLogger();
    }
}
```

Note, don't forget to add `.UseSerilog` in the `Program.cs`.

```csharp
using Microsoft.AspNetCore.Hosting;
using Serilog;

public class Program
{
    public static void Main(string[] args)
    {
        CreateWebHostBuilder(args).Build().Run();
    }

    private static WebHostBuilder CreateWebHostBuilder(string[] args)
    {
        return WebHost.CreateDefaultBuilder(args)
                      .UseSerilog()
                      .UseStartup<Startup>();
    }
}
```

To have access to the `.AzureApplicationInsights` extension, make sure you've installed [Arcus.Observability.Telemetry.Serilog.Sinks.ApplicationInsights](https://www.nuget.org/packages/Arcus.Observability.Telemetry.Serilog.Sinks.ApplicationInsights/).

[&larr; back](/)
