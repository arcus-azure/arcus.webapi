---
title: "Correlate between HTTP requests/responses via ASP.NET Core middleware"
layout: default
---

# Correlation Between HTTP Requests

The `Arcus.WebApi.Correlation` library provides a way to add correlation between HTTP requests. 
This correlation happens with the use of two HTTP request/response headers: `RequestId` and `X-Transaction-ID`.

## How This Works

When an application is configured to use the default configuration of the correlation, each HTTP response will get an extra header called `RequestId` containing an unique identifier to distinguish between requests/responses.
The `X-Transaction-ID` can be overriden by the request, meaning: if the HTTP request already contains a `X-Transaction-ID` header, the same header+value will be used in the HTTP response.

Additional [configuration](#configuration) is available to tweak this functionality.

## Installation

This feature requires to install our NuGet package

```shell
PM > Install-Package Arcus.WebApi.Correlation
```

## Usage

To make sure the correlation is added to the HTTP response, following additions have to be made in the `.ConfigureServices` and `Configure` methods:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddCorrelation();
}

public void Configure(IApplicationBuilder app, IHostingEnvironment env)
{
    app.UserCorrelation();

    app.UseMvc();
}
```

Note: because the correlation is based on <span>ASP.NET</span> Core middleware, it's recommended to place it before the `.UseMvc` call.

## Configuration

Some extra options are available to alter the functionality of the correlation:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddCorrelation(options =>
    {
        // Configuration on the transaction ID (`X-Transaction-ID`) request/response header.
        // ---------------------------------------------------------------------------------

        // Whether the transaction ID can be specified in the request, and will be used throughout the request handling.
        // The request will return early when the `.AllowInRequest` is set to `false` and the request does contain the header.
        options.Transaction.AllowInRequest = true;

        // Whether or not the transaction ID should be generated when there isn't any transaction ID found in the request.
        // When the `.GenerateWhenNotSpecified` is set to `false` and the request doesn't contain the header, no value will be available for the transaction ID; 
        // otherwise a GUID will be generated.
        options.Transaction.GenerateWhenNotSpecified = true;

        // Whether to include the transaction ID in the response.
        options.Transaction.IncludeInResponse = true;

        // The header to look for in the HTTP request, and will be set in the HTTP response.
        options.Transaction.HeaderName = "X-Transaction-ID";

        // Configuration on the operation ID (`RequestId`) response header.
        // ----------------------------------------------------------------

        // Whether to include the operation ID in the response.
        options.Operation.IncludeInResponse = true;

        // The header that will contain the operation ID in the HTTP response.
        options.Operation.HeaderName = "RequestId";
    });
}
```

[&larr; back](/)