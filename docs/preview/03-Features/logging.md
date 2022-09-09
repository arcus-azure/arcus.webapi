---
title: "Logging in Web API applications"
layout: default
---

# Logging in Web API applications

The `Arcus.WebApi.Logging` package provides a way to log several kinds of information during the receival and handling of HTTP requests.

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

To use this middleware, add the following line of code:

```csharp
using Microsoft.AspNetCore.Builder;

WebApplicationBuilder builder = WebApplication.CreateBuilder();
WebApplication app = builder.Build();

app.UseRouting();

app.UseExceptionHandling();

app.UseEndpoints(endpoints => endpoints.MapControllers());
```

If you have multiple middleware components configured, it is advised to put the `ExceptionHandlingMiddleware` as soon as possible.
By doing so, unhandled exceptions that might occur in other middleware components will also be logged by the `ExceptionHandlingMiddleware` component.

### Configuration

When custom exception handling is required, you can inherit from the `ExceptionHandlingMiddleware` to create your own middleware component and register it with Arcus' extension.

This example implements the exception handling middleware to influence the log message and adds a custom determination of the HTTP response status code.

```csharp
using Arcus.WebApi.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

public class MyExceptionHandlingMiddleware : ExceptionHandlingMiddleware
{
    public MyExceptionHandlingMiddleware(RequestDelegate next) : base(next)
    {
    }

    protected override void LogException(ILogger logger, Exception exception)
    {
        logger.LogCritical(exception, "Custom exception handling!");
    }

    protected override void WriteFailureToResponse(Exception exception, HttpStatusCode defaultFailureStatusCode, HttpContext context)
    {
        if (exception is ValidationException)
        {
            context.Response.StatusCode = (int) HttpStatusCode.BadRequest;
        }
        else if (exception is TimeoutException)
        {
            context.Response.StatusCode = (int) HttpStatusCode.ServerTimeout;
        }
        else 
        {
            context.Response.StatusCode = (int) defaultFailureStatusCode;
        }
    }
}
```

Next, make sure that you pass along the exception middleware to the exception handling extension.

```csharp
using Microsoft.AspNetCore.Builder;

WebApplicationBuilder builder = WebApplication.CreateBuilder();
WebApplication app = builder.Build();

app.UseExceptionHandling<MyExceptionHandlingMiddleware>();
```

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

WebApplicationBuilder builder = WebApplication.CreateBuilder();
WebApplication app = builder.Build();

app.UseRouting();

app.UseRequestTracking();
app.UseExceptionHandling();

app.UseEndpoints(endpoints => endpoints.MapControllers());
```

### Configuration

The request tracking middleware has several configuration options to manipulate what the request logging emits should contain.

```csharp
using Microsoft.AspNetCore.Builder;

WebApplicationBuilder builder = WebApplication.CreateBuilder();
WebApplication app = builder.Build();

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

    // Size of the response body buffer (in bytes) which should be tracked. Response bodies greater than this buffer will only be partly present in the telemetry.
    // (default: no limit)
    options.ResponseBodyBufferSize = 10 * 1024 * 1024; // 10MB

    // All response HTTP status codes that should be tracked. If not defined, all status codes are considered included and will all be tracked.
    // (default: empty list, which means all will be tracked)
    // Following example will change the default behavior so that only HTTP responses with the status code 500 `InternalServerError` will be tracked.
    options.TrackedStatusCodes.Add(HttpStatusCode.InternalServerError);

    // All response HTTP status code ranges that should be tracked. If not defined, all status codes are considered included and will be tracked.
    // (default: empty list, which means all will be tracked)
    // Following example will change the default behavior so that only HTTP response status codes in the range of 500 to 599 (inconclusive) will be tracked.
    options.TrackedStatusCodeRanges.Add(new StatusCodeRange(500, 599));

     // All omitted HTTP routes that should be excluded from the request tracking logging emits.
    // (default: no routes).
    options.OmittedRoutes.Add("/api/v1/health");
});

app.UseEndpoints(endpoints => endpoints.MapControllers());
```

### Excluding certain routes

You can opt-out for request tracking on one or more endpoints (operation and/or controller level).
This can come in handy if you have certain endpoints with rather large request bodies or want to boost performance or are constantly probed to monitor the application.
This can easily be done by using the `ExcludeRequestTrackingAttribute` on the endpoint or controller of your choosing.

```csharp
using Arcus.WebApi.Logging;

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

#### Excluding request/response bodies on specific routes

When used as in the example above, then all routes of the controller will be excluded from the telemetry tracking. 
The exclude attribute allows you to specify on a more specific level what part of the request should be excluded.

```csharp
using Arcus.WebApi.Logging;

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

#### Including HTTP status codes/status code ranges on specific routes

With the `RequestTracking` attribute, you can include a fixed HTTP status code or a range of HTTP status codes on a specific route.
This allows for a more finer grained control of the request tracking than to specify these status codes in the request tracking options.

```csharp
using System.Net;
using Arcus.WebApi.Logging;

[ApiController]
public class OrderController : ControllerBase
{
    // Only when the response returns a 500 `InternalServerError` will the request tracking occur for this endpoint.
    [HttpPost]
    [RequestTracking(HttpStatusCode.InternalServerError)]
    public IActionResult PostThatTracksInternalServerError()
    {
        return Ok();
    }

    // Only when the response returns a HTTP status code in the range of 500 to 599 (inconclusive) wll the request tracking occur for this endpoint.
    [HttpPost]
    [RequestTracking(500, 599)]
    public IActionResult PostThatTracksServerErrors()
    {
        return Ok();
    }
}
```

### Customization

Optionally, one can inherit from this middleware component and override several default functionality:
- by default, all the request headers (except some known authentication headers) are tracked
- by default, when tracking the request body, the entire body is tracked
- by default, when tracking the response body, the entire body is tracked

Following example shows how specific headers can be redacted without omitting them:

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

    // Also available to be overridden:
    // `SanitizeRequestBody`
    // `SanitizeResponseBody`
}
```

And, configure your custom middleware like this in the `Startup.cs`:

```csharp
using Microsoft.AspNetCore.Builder;

WebApplicationBuilder builder = WebApplication.CreateBuilder();
WebApplication app = builder.Build();

app.UseRouting();

app.UseRequestTracking<EmptyButNotOmitRequestTrackingMiddleware>(options => options.OmittedHeaderNames.Clear());

app.UseEndpoints(endpoints => endpoints.MapControllers());
```

So the resulting log message becomes:

`HTTP Request POST http://localhost:5000/weatherforecast completed with 200 in 00:00:00.0191554 at 03/23/2020 10:12:55 +00:00 - (Context: [X-Api-Key,])`

#### Reducing requests to specific HTTP status code(s)

By default, all the responded HTTP status codes will be tracked but this can be altered according to your choosing.

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

        // Request tracking will happened on this response.
        return Created($"/orders/{order.Id}", order);
    }
}
```

Let's change the request tracking to only track successful '201 Created' responses.
This can be changed via the options:

```csharp
using Microsoft.AspNetCore.Builder;

WebApplicationBuilder builder = WebApplication.CreateBuilder();
WebApplication app = builder.Build();

app.UseRouting();

app.UseRequestTracking(options => options.TrackedStatusCodes.Add(HttpStatusCode.Created));

app.UseEndpoints(endpoints => endpoints.MapControllers());
```

This means that every endpoint will only track `201 Created` responses. Changing this in the options is usually for when you want to streamline your entire application to only track a certain set of status codes.
More fine grained control can be achieved via placing an attribute on either the controller's class definition or the endpoint method:

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
            // No request tracking will appear in the logs.
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

WebApplicationBuilder builder = WebApplication.CreateBuilder();

builder.Services.AddSingleton<IAppVersion, MyAppVersion>();

WebApplication app = builder.Build();

// Uses the previously registered `IAppVersion` service to include the application to the default `X-Version` response header.
app.UseVersionTracking();

// Uses the previously registered `IAppVersion` service to include the application to the custom `X-My-Version` response header.
app.UseVersionTracking(options => options.Header = "X-My-Version");
```

## Application Insights

To get the information logged in Azure Application Insights, configure logging like this when building the `WebApplication`:

```csharp
using Microsoft.AspNetCore.Builder;
using Serilog;
using Serilog.Configuration;

WebApplicationBuilder builder = WebApplication.CreateBuilder();

builder.Host.UseSerilog((context, serviceProvider, config) =>
{
    return new LoggerConfiguration()
        .WriteTo.AzureApplicationInsights("my-instrumentation-key")
        .CreateLogger();
});

WebApplication app = builder.Build();
```

To have access to the `.AzureApplicationInsights` extension, make sure you've installed [Arcus.Observability.Telemetry.Serilog.Sinks.ApplicationInsights](https://www.nuget.org/packages/Arcus.Observability.Telemetry.Serilog.Sinks.ApplicationInsights/).

[&larr; back](/)
