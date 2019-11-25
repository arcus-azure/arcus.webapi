---
title: "Authentication with shared access keys via ASP.NET Core authentication filters"
layout: default
---

# Authentication with shared access keys

The `Arcus.WebApi.Security` package provides a mechanism that uses shared access keys to grant access to a web application.
This authentication process consists of two parts:

1. Find the configured HTTP request header or the configured Query parameter that contains the shared access key
2. Shared access key matches the value with the secret stored, determined via configured secret provider

The package allows two ways to configure this type of authentication mechanmism in an <span>ASP.NET</span> application:
- [Global Shared access key authentication](#globally-enforce-shared-access-key-authentication)
- [Shared access key authentication per controller or operation](#enforce-shared-access-key-authentication-per-controller-or-operation)

## Installation

This feature requires to install our NuGet package

```shell
PM > Install-Package Arcus.WebApi.Security.Authentication
```
 
## Globally enforce shared access key authentication

### Introduction

The `SharedAccessKeyAuthenticationFilter` can be added to the request filters in an <span>ASP.NET</span> Core application.
This filter will then add authentication to all endpoints via a shared access key configurable on the filter itself.

### Usage

The authentication requires an `ICachedSecretProvider` or `ISecretProvider` dependency to be registered with the services container of the ASP.NET request pipeline.  This is typically done in the `ConfigureServices` method of the `Startup` class.
Once this is done, the `SharedAccessKeyAuthenticationFilter` can be added to the filters that will be applied to all actions:

```csharp
public void ConfigureServices(IServiceCollections services)
{
    services.AddSingleton<ICachedSecretProvider>(serviceProvider => new MyCachedSecretProvider());
    services.AddMvc(options => options.Filters.Add(new SharedAccessKeyAuthenticationFilter(headerName: "http-request-header-name", queryParameterName: "api-key", secretName: "shared-access-key-name")));
}
```

## Enforce shared access key authentication per controller or operation

### Introduction

The `SharedAccessKeyAuthenticationAttribute` can be added on both controller- and operation level in an <span>ASP.NET</span> Core application.
The shared access key authentication will then be applied to the endpoint(s) that are decorated with the `SharedAccessKeyAuthenticationAttribute`.

### Usage

The authentication requires an `ICachedSecretProvider` or `ISecretProvider` dependency to be registered with the services container of the ASP.NET request pipeline.  This is typically done in the `ConfigureServices` method of the `Startup` class:

```csharp
public void ConfigureServices(IServiceCollections services)
{
    services.AddSingleton<ICachedSecretProvider>(serviceProvider => new CachedSecretProvider(new MySecretProvider()));
    services.AddMvc();
}
```

After that, the `SharedAccessKeyAuthenticationAttribute` attribute can be applied on the controllers, or if more fine-grained control is needed, on the operations that requires authentication:

```csharp
[ApiController]
[SharedAccessKeyAuthentication(headerName: "http-request-header-name", queryParameterName: "api-key", secretName: "shared-access-key-name")]
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

[&larr; back](/)
