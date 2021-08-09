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

These features require to install our NuGet package

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
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

public class Startup
{
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
       app.UseMiddleware<Arcus.WebApi.Logging.ExceptionHandlingMiddleware>();

       ...
       app.UseMvc();
    }
}
```

If you have multiple middleware components configured, it is advised to put the `ExceptionHandlingMiddleware` as the first one.
By doing so, unhandled exceptions that might occur in other middleware components will also be logged by the `ExceptionHandlingMiddleware` component.

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
