---
title: "Authentication with shared access keys via ASP.NET Core authentication filters"
layout: default
---

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


