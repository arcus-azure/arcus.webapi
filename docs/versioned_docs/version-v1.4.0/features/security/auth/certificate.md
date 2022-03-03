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
- [Authentication with certificate](#authentication-with-certificate)
  - [Installation](#installation)
  - [Globally enforce certificate authentication](#globally-enforce-certificate-authentication)
    - [Introduction](#introduction)
    - [Usage](#usage)
  - [Enforce certificate authentication per controller or operation](#enforce-certificate-authentication-per-controller-or-operation)
    - [Introduction](#introduction-1)
    - [Usage](#usage-1)
      - [Configuration](#configuration)
  - [Bypassing authentication](#bypassing-authentication)

## Installation

This feature requires to install our NuGet package

```shell
PM > Install-Package Arcus.WebApi.Security
```

## Globally enforce certificate authentication

### Introduction

The `CertificateAuthenticationFilter` can be added to the request filters in an <span>ASP.NET</span> Core application.
This filter will then add authentication to all endpoints via one or many certificate properties configurable on the filter itself.

### Usage

The authentication requires a service dependency to be registered with the services container of the <span>ASP.NET</span> request pipeline, which can be one of the following:
- Arcus secret store: see [our official documentation](https://security.arcus-azure.net/features/secret-store/) for more information about setting this up.
- `Configuration`: key/value pairs in the configuration of the <span>ASP.NET</span> application.
- `IX509ValidationLocation`/`X509ValidationLocation`: custom or built-in implementation that retrieves the expected certificate values.

This registration of the service is typically done in the `ConfigureServices` method of the `Startup` class.

Each certificate property that should be validated can use a different service dependency. 
This mapping of what service which property uses, is defined in an `CertificateAuthenticationValidator` instance.

Once this is done, the `CertificateAuthenticationFilter` can be added to the filters that will be applied to all actions:

```csharp
using Arcus.WebApi.Security.Authentication.Certificates;
using Microsoft.Extensions.DependencyInjection;

public class Startup
{
    public void ConfigureServices(IServiceCollections services)
    {
        // See https://security.arcus-azure.net/features/secret-store/ for more information.
        services.AddSecretStore(stores =>  stores.AddAzureKeyVaultWithManagedIdentity("https://your-keyvault.vault.azure.net/", CacheConfiguration.Default));

        var certificateValidator =
            new CertificateAuthenticationValidator(
                new CertificateAuthenticationConfigBuilder()
                    .WithIssuer(X509ValidationLocation.SecretProvider, "key-to-certificate-issuer-name")
                    .Build());

        services.AddSingleton(certificateValidator);

        services.AddMvc(mvcOptions => 
        {
            // Adds certificate authentication to the request pipeline.
            mvcOptions.Filters.AddCertificateAuthentication());

            // Additional consumer-configurable options to change the behavior of the authentication filter.
            mvcOptions.Filters.AddCertificateAuthentication(configureOptions: options =>
            {
                // Adds certificate authentication to the request pipeline with emitting security events during the authorization of the request.
                // (default: `false`)
                options.EmitSecurityEvents = true;
            }));
        });
    }
}
```

## Enforce certificate authentication per controller or operation

### Introduction

The `CertificateAuthenticationAttribute` can be added on both controller- and operation level in an <span>ASP.NET</span> Core application.
This certificate authentication will then be applied to the endpoint(s) that are decorated with the `CertificateAuthenticationAttribute`.

### Usage

The authentication requires a service dependency to be registered with the services container of the <span>ASP.NET</span> request pipeline, which can be one of the following:
- Arcus secret store: see [our official documentation](https://security.arcus-azure.net/features/secret-store/) for more information about setting this up.
- `Configuration`: key/value pairs in the configuration of the <span>ASP.NET</span> application.
- `IX509ValidationLocation`/`X509ValidationLocation`: custom or built-in implementation that retrieves the expected certificate values

This registration of the service is typically done in the `ConfigureServices` method of the `Startup` class:

```csharp
using Arcus.WebApi.Security.Authentication.Certificates;
using Microsoft.Extensions.DependencyInjection;

public class Startup
{
    public void ConfigureServices(IServiceCollections services)
    {
        // See https://security.arcus-azure.net/features/secret-store/ for more information.
        services.AddSecretStore(stores =>  stores.AddAzureKeyVaultWithManagedIdentity("https://your-keyvault.vault.azure.net/", CacheConfiguration.Default));

        var certificateValidator = 
            new CertificateAuthenticationValidator(
                new CertificateAuthenticationConfigBuilder()
                    .WithIssuer(X509ValidationLocation.SecretProvider, "key-to-certificate-issuer-name")
                    .Build());

        services.AddSingleton(certificateValidator);
    
        services.AddMvc();
    }
}
```

After that, the `CertificateAuthenticationAttribute` attribute can be applied on the controllers, or if more fine-grained control is needed, on the operations that requires authentication:

```csharp
using Arcus.WebApi.Security.Authentication.Certificates;

[ApiController]
[CertificateAuthentication]
public class MyApiController : ControllerBase
{
    [HttpGet]
    [Route("authz/certificate")]
    public IActionResult AuthorizedGet()
    {
        return Ok();
    }
}
```

#### Configuration

Some additional configuration options are available on the attribute.

```csharp
// Adds certificate authentication to the request pipeline with emitting of security events during the authentication of the request.
// (default: `false`)
[CertificateAuthentication(EmitSecurityEvents = true)]
```

## Bypassing authentication

The package supports a way to bypass the certificate authentication for certain endpoints.
This works with adding one of these attributes to the respectively endpoint:
- `BypassCertificateAuthentication`
- `AllowAnonymous`

> Works on both method and controller level, using either the certificate filter or attribute.

```csharp
using Arcus.WebApi.Security.Authentication.Certificates;

[ApiController]
[CertificateAuthentication]
public class SystemController : ControllerBase
{
    [HttpGet('api/v1/health')]
    [BypassCertificateAuthentication]
    public IActionResult GetHealth()
    {
        return Ok();
    }
}
```


[&larr; back](/)
