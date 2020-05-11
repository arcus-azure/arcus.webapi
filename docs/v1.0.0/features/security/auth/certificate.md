---
title: "Authentication with certificate via ASP.NET Core authentication filters"
layout: default
---

# Authentication with certificate

The `Arcus.WebApi.Security` package provides a mechanism that uses the client certificate of the request to grant access to a web application.

This authentication process consists of following parts:

1. Find the client certificate configured on the HTTP request
2. Determine which properties of the received client certificate are used for authentication
3. The property value(s) of the client certificate matches the value(s) determined via configured secret provider, configuration or custom implementation

The package allows two ways to configure this type of authentication mechanism in an <span>ASP.NET</span> application:
- [Globally enforce certificate authentication](#Globally-enforce-certificate-authentication)
- [Enforce certificate authentication per controller or operation](#Enforce-certificate-authentication-per-controller-or-operation)

## Installation

This feature requires to install our NuGet package

```shell
PM > Install-Package Arcus.WebApi.Security.Authentication
```

## Globally enforce certificate authentication

### Introduction

The `CertificateAuthenticationFilter` can be added to the request filters in an <span>ASP.NET</span> Core application.
This filter will then add authentication to all endpoints via one or many certificate properties configurable on the filter itself.

### Usage

The authentication requires a service dependency to be registered with the services container of the <span>ASP.NET</span> request pipeline, which can be one of the following:
- `ICachedSecretProvider` or `ISecretProvider`: built-in or you implementation of the secret provider.
- `Configuration`: key/value pairs in the configuration of the <span>ASP.NET</span> application.
- `IX509ValidationLocation`/`X509ValidationLocation`: custom or built-in implementation that retrieves the expected certificate values.

This registration of the service is typically done in the `ConfigureServices` method of the `Startup` class.

Each certificate property that should be validated can use a different service dependency. 
This mapping of what service which property uses, is defined in an `CertificateAuthenticationValidator` instance.

Once this is done, the `CertificateAuthenticationFilter` can be added to the filters that will be applied to all actions:

```csharp
public void ConfigureServices(IServiceCollections services)
{
    services.AddScoped<ICachedSecretProvider(serviceProvider => new MyCachedSecretProvider());

    var certificateAuthenticationConfig = 
        new CertificateAuthenticationConfigBuilder()
            .WithIssuer(X509ValidationLocation.SecretProvider, "key-to-certificate-issuer-name")
            .Build();
    
    services.AddScoped<CertificateAuthenticationValidator>(
        serviceProvider => new CertificateAuthenticationValidator(certificateAuthenticationConfig));

    services.AddMvc(
        options => options.Filters.Add(new CertificateAuthenticationFilter()));
}
```

## Enforce certificate authentication per controller or operation

### Introduction

The `CertificateAuthenticationAttribute` can be added on both controller- and operation level in an <span>ASP.NET</span> Core application.
This certificate authentication will then be applied to the endpoint(s) that are decorated with the `CertificateAuthenticationAttribute`.

### Usage

The authentication requires a service dependency to be registered with the services container of the <span>ASP.NET</span> request pipeline, which can be one of the following:
- `ICachedSecretProvider` or `ISecretProvider`: built-in or you implementation of the secret provider.
- `Configuration`: key/value pairs in the configuration of the <span>ASP.NET</span> application.
- `IX509ValidationLocation`/`X509ValidationLocation`: custom or built-in implementation that retrieves the expected certificate values

This registration of the service is typically done in the `ConfigureServices` method of the `Startup` class:

```csharp
public void ConfigureServices(IServiceCollections services)
{
    services.AddScoped<ICachedSecretProvider(serviceProvider => new MyCachedSecretProvider());

    var certificateAuthenticationConfig = 
        new CertificateAuthenticationConfigBuilder()
            .WithIssuer(X509ValidationLocation.SecretProvider, "key-to-certificate-issuer-name")
            .Build();

    services.AddScoped<CertificateAuthenticationValidator>(
        serviceProvider => new CertificateAuthenticationValidator(certificateAuthenticationConfig));
 
    services.AddMvc();
}
```

After that, the `CertificateAuthenticationAttribute` attribute can be applied on the controllers, or if more fine-grained control is needed, on the operations that requires authentication:

```csharp
[ApiController]
[CertificateAuthentication]
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

[&larr; back](/)
