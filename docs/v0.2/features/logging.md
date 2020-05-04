---
title: "Logging unhandled exceptions via ASP.NET Core middleware"
layout: default
---

# Logging unhandled exceptions

![](https://img.shields.io/badge/Available%20starting-v0.1-green?link=https://github.com/arcus-azure/arcus.webapi/releases/tag/v0.1.0)

The `ExceptionHandlingMiddleware` class can be added to the <span>ASP.NET</span> Core pipeline to log unhandled exceptions that are thrown during request processing.
The unhandled exceptions are caught by this middleware component and are logged through the `ILogger` implementations that are configured inside the project.

## Installation

This feature requires to install our NuGet package

```shell
PM > Install-Package Arcus.WebApi.Logging -Version 0.2.0
```

## Usage

To use this middleware, add the following line of code in the `Startup.Configure` method:

```csharp
public void Configure(IApplicationBuilder app, IHostingEnvironment env)
{
   app.UseMiddleware<Arcus.WebApi.Logging.ExceptionHandlingMiddleware>();

   ...
   app.UseMvc();
}
```

If you have multiple middleware components configured, it is advised to put the `ExceptionHandlingMiddleware` as the first one.  By doing so, unhandled exceptions that might occur in other middleware components will also be logged by the `ExceptionHandlingMiddleware` component.

To get the exceptions logged in Azure Application Insights, configure logging like this when building the `IWebHost` in `Program.cs`:

```csharp
WebHost.CreateDefaultBuilder(args)
       .UseApplicationInsights()
       .ConfigureLogging(loggers => loggers.AddApplicationInsights())
       .UseStartup<Startup>();
```

To be able to use the `AddApplicationInsights` extension method, the Microsoft.Extensions.Logging.ApplicationInsights package must be installed.


If the parameter-less `AddApplicationInsights` method is used, the configurationsetting `ApplicationInsights:InstrumentationKey` must be specified and the value of the instrumentation-key of the Application Insights resource must be set.

[&larr; back](/)