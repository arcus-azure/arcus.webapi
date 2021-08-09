---
title: "Authentication with shared access keys via ASP.NET Core authentication filters"
layout: default
---

## Shared access key authentication

The `Arcus.WebApi.Security` package provides an mechanism that uses shared access keys to authenticate users.
This authentication process consists of two parts:

1. Looks for the configured HTTP request header that contains the shared access key
2. Shared access key matches the value with the secret stored, determined via configured secret provider

The package allows two ways to configure this type of authentication mechanmism in an <span>ASP.NET</span> application:
- [Global Shared access key authentication](#globally-enforce-shared-access-key-authentication)
- [Shared access key authentication per controller or operation](#enforce-shared-access-key-authentication-per-controller-or-operation)

## Installation

This feature requires to install our NuGet package

```shell
PM > Install-Package Arcus.WebApi.Security.Authentication -Version 0.2.0
```

## Globally enforce shared access key authentication

### Introduction

The `SharedAccessKeyAuthenticationFilter` can be added to the request filters in an <span>ASP.NET</span> Core application.
This filter will then add authetication to all routes via a shared access key configurable on the filter itself.

### Usage

The authentication requires a `ICachedSecretProvider` or `ISecretProvider` to be registered in services of the applications (normally in the `Startup` class).
After that, you can add the filter to the MVC services:

```csharp
using Arcus.Security.Core.Caching;
using Arcus.WebApi.Security.Authentication.SharedAccessKey;
using Microsoft.Extensions.DependencyInjection;

public class Startup
{
    public void ConfigureServices(IServiceCollections services)
    {
        services.AddSingleton<ICachedSecretProvider>(serviceProvider => new MyCachedSecretProvider());
        services.AddMvc(options => options.Filters.Add(new SharedAccessKeyAuthenticationFilter(headerName: "http-request-header-name", secretName: "shared-access-key-name")));
    }
}
```

## Enforce shared access key authentication per controller or operation

### Introduction

The `SharedAccessKeyAuthenticationAttribute` can be added on both `Controller` and method level in an <span>ASP.NET</span> Core application.
This attribute will then add authentication to the route(s) via shared access keys configurable on the attribute itself.

### Usage

The authentication requires a `ICachedSecretProvider` or `ISecretProvider` to be registered in services of the applications (normally in the `Startup` class):

```csharp
using Arcus.Security.Core.Caching;
using Microsoft.Extensions.DependencyInjection;

public class Startup
{
    public void ConfigureServices(IServiceCollections services)
    {
        services.AddSingleton<ICachedSecretProvider>(serviceProvider => new CachedSecretProvider(new MySecretProvider()));
        services.AddMvc();
    }
}
```

After that, you can freely use the attribute on the route (single method) or routes (multiple methods or `Controller` level) that requires authentication:

```csharp
using Arcus.WebApi.Security.Authentication;

[ApiController]
[SharedAccessKeyAuthentication(headerName: "http-request-header-name", secretName: "shared-access-key-name")]
public class MyApiController : ControllerBase
{
    [HttpGet]
    [Route("authz/shared-access-key")]
    public Task<IActionResult> AuthorizedGet()
    {
        return Task.FromResult<IActionResult>(Ok());
    }
}
```