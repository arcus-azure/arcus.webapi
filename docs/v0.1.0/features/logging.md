---
title: "Logging unhandled exceptions via ASP.NET Core middleware"
layout: default
---

## Logging unhandled exceptions

### Introduction

The `ExceptionHandlingMiddleware` class can be added to the <span>ASP.NET</span> Core pipeline to log unhandled exceptions that are thrown during request processing.
The unhandled exceptions are caught by this middleware component and are logged through the `ILogger` implementations that are configured inside the project.

### Installation

This feature requires to install our NuGet package

```shell
PM > Install-Package Arcus.WebApi.Logging -Version 0.1.0
```

### Usage

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

If you have multiple middleware components configured, it is advised to put the `ExceptionHandlingMiddleware` as the first one.  By doing so, unhandled exceptions that might occur in other middleware components will also be logged by the `ExceptionHandlingMiddleware` component.

To get the exceptions logged in Azure Application Insights, configure logging like this when building the `IWebHost` in `Program.cs`:

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
                      .UseApplicationInsights()
                      .ConfigureLogging(loggers => loggers.AddApplicationInsights())
                      .UseStartup<Startup>();
    }
}
```

To be able to use the `AddApplicationInsights` extension method, the Microsoft.Extensions.Logging.ApplicationInsights package must be installed.


If the parameter-less `AddApplicationInsights` method is used, the configurationsetting `ApplicationInsights:TelemetryKey` must be specified and the value of the telemetry-key of the Application Insights resource must be set.