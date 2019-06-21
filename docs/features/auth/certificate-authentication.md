---
title: "Authentication with certificate via ASP.NET Core authentication filters"
layout: default
---

The `Arcus.WebApi.Security` package provides a mechanism that uses the client certificate of the request to grant access to a web application.
This authentication process consists of following parts:

1. Find the client certificate configured on the HTTP request
2. Determine which properties of the received client certificate are used for authentication
3. The property value(s) of the client certificate matches the value(s) determined via configured secret provider

The package allows two ways to configure this type of authentication mechanism in an <span>ASP.NET</span> application:
- [Globally enforce certificate authentication](#Globally-enforce-certificate-authentication)
- [Enforce certificate authentication per controller or operation](#Enforce-certificate-authentication-per-controller-or-operation)

## Globally enforce certificate authentication

### Introduction

The `CertificateAuthenticationFilter` can be added to the request filters in an <span>ASP.NET</span> Core application.
This filter will then add authentication to all endpoints via one or many certificate properties configurable on the filter itself.

### Usage

The authentication requires an `ICachedSecretProvider` or `ISecretProvider` dependency to be registered with the services container of the <span>ASP.NET</span> request pipeline. This is typically done in the `ConfigureServices` method of the `Startup` class.
Once this is done, the `CertificateAuthenticationFilter` can be added to the filters that will be applied to all actions:

```csharp
public void ConfigureServices(IServiceCollections services)
{
    services.AddScoped<ICachedSecretProvider(serviceProvider => new MyCachedSecretProvider());
    services.AddMvc(
        options => options.Filters.Add(
            new CertificateAuthenticationFilter(
                X509CertificateRequirement.SubjectName,
                "key-to-certificate-subject-name"
            )));
}
```

## Enforce certificate authentication per controller or operation

### Introduction

The `CertificateAuthenticationAttribute` can be added on both controller- and operation level in an <span>ASP.NET</span> Core application.
This certificate authentication will then be applied to the endpoint(s) that are decorated with the `CertificateAuthenticationAttribute`.

### Usage

The authentication requires an `ICachedSecretProvider` or `ISecretProvider` dependency to be registered with the services container of the <span>ASP.NET</span> request pipeline. This is typically done in the `ConfigureServices` method of the `Startup` class:

```csharp
public void ConfigureServices(IServiceCollections services)
{
    services.AddScoped<ICachedSecretProvider>(serviceProvider => new CachedSecretProvider(new MySecretProvider()));
    services.AddMvc();
}
```

After that, the `CertificateAuthenticationAttribute` attribute can be applied on the controllers, or if more fine-grained control is needed, on the operations that requires authentication:

```csharp
[ApiController]
[CertificateAuthentication(X509CertificateRequirement.IssuerName, "key-to-certificate-issuer-name")]
public class MyApiController : ControllerBase
{
    [HttpGet]
    [Route("authz/certificate")]
    public Task<IActionResult> AuthorizedGet()
    {
        return Task.FromResult<IActionResult>(Ok());
    }
}
```
