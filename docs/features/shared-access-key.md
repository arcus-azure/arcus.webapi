---
title: "Authentication with shared access keys via ASP.NET Core authentication filters"
layout: default
---

## Arcus.WebApi.Security.SharedAccessKeyAuthentication

The `Arcus.WebApi.Security` package provides an mechanism that uses shared access keys to authenticate users.
This authentication process consists of two parts:

1. Looks for the configured HTTP request header that contains the shared access key
2. Matches this value with the configured **Key Vault** secret

The package allows two ways to configure this type of authentication mechanmism in an <span>ASP.NET</span> application:
- [Shared access key attribute](#ArcusWebApiSecuritySharedAccessKeyAuthenticationAttribute) allows local authentication on  `Controller` or method level
- [Shared access key filter](#ArcusWebApiSecuritySharedAccessKeyAUthenticationFilter) allows global authentication 

## Arcus.WebApi.Security.SharedAccessKeyAuthenticationAttribute

### Introduction

The `SharedAccessKeyAuthenticationAttribute` can be added on both `Controller` and method level in an <span>ASP.NET</span> Core application.
This attribute will then add authentication to the route(s) via shared access keys configurable on the attribute itself.

### Usage

The authentication requires a `ICachedSecretProvider` or `ISecretProvider` to be registered in services of the applications (normally in the `Startup` class):

```csharp
public void ConfigureServices(IServiceCollections services)
{
    services.AddScoped<ICachedSecretProvider>(serviceProvider => new CachedSecretProvider(new MySecretProvider()));
    services.AddMvc();
}
```

After that, you can freely use the attribute on the route (single method) or routes (multiple methods or `Controller` level) that requires authentication:

```csharp
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

## Arcus.WebApi.Security.SharedAccessKeyAuthenticationFilter

### Introduction

The `SharedAccessKeyAuthenticationFilter` can be added to the request filters in an <span>ASP.NET</span> Core application.
This filter will then add authetication to all routes via a shared access key configurable on the filter itself.

### Usage

The authentication requires a `ICachedSecretProvider` or `ISecretProvider` to be registered in services of the applications (normally in the `Startup` class).
After that, you can add the filter to the MVC services:

```csharp
public void ConfigureServices(IServiceCollections services)
{
    services.AddScoped<ICachedSecretProvider>(serviceProvider => new MyCachedSecretProvider());
    services.AddMvc(options => options.Filters.Add(new SharedAccessKeyAuthenticationFilter(headerName: "http-request-header-name", secretName: "shared-access-key-name")));
}
```


