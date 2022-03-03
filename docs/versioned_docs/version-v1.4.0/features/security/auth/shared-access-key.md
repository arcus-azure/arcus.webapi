---
title: "Authentication with shared access keys via ASP.NET Core authentication filters"
layout: default
---

# Authentication with shared access keys

The `Arcus.WebApi.Security` package provides a mechanism that uses shared access keys to grant access to a web application.
This authentication process consists of two parts:

1. Find the configured parameter that holds the shared access key, this can be a request header, a query parameter or both.
2. Shared access key matches the value with the secret stored, determined via configured secret provider

The package allows two ways to configure this type of authentication mechanism in an <span>ASP.NET</span> application:
- [Authentication with shared access keys](#authentication-with-shared-access-keys)
  - [Installation](#installation)
  - [Globally enforce shared access key authentication](#globally-enforce-shared-access-key-authentication)
    - [Introduction](#introduction)
    - [Usage](#usage)
  - [Enforce shared access key authentication per controller or operation](#enforce-shared-access-key-authentication-per-controller-or-operation)
    - [Introduction](#introduction-1)
    - [Usage](#usage-1)
      - [Configuration](#configuration)
  - [Behavior in validating shared access key parameter](#behavior-in-validating-shared-access-key-parameter)
  - [Bypassing authentication](#bypassing-authentication)

## Installation

This feature requires to install our NuGet package

```shell
PM > Install-Package Arcus.WebApi.Security
```
 
## Globally enforce shared access key authentication

### Introduction

The `SharedAccessKeyAuthenticationFilter` can be added to the request filters in an <span>ASP.NET</span> Core application.
This filter will then add authentication to all endpoints via a shared access key configurable on the filter itself.

### Usage

We created a `SharedAccessKeyAuthenticationFilter` MVC filter which will be applied to all actions:

```csharp
using Arcus.WebApi.Security.Authentication.SharedAccessKey;
using Microsoft.Extensions.DependencyInjection;

public class Startup
{
    public void ConfigureServices(IServiceCollections services)
    {
        // See https://security.arcus-azure.net/features/secret-store/ for more information.
        services.AddSecretStore(stores =>  stores.AddAzureKeyVaultWithManagedIdentity("https://your-keyvault.vault.azure.net/", CacheConfiguration.Default));

        services.AddMvc(options => 
        {
            // Adds shared access key authentication to the request pipeline where the request query string parameter will be verified 
            // if the query parameter value contain the expected secret value, retrievable with the given secret name.
            options.Filters.AddSharedAccessAuthenticationOnQuery(
                queryParameterName: "api-key", 
                secretName: "shared-access-key-name")));

            // Adds shared access key authentication to the request pipeline where only the request header will be verified if it contains the expected secret value.
            options.Filters.AddSharedAccessAuthenticationOnHeader(
                headerName: "http-request-header-name",
                secretName: "shared-access-key-name"));

            // Additional consumer-configurable options to change the behavior of the authentication filter.
            options.Filters.AddSharedAccessAuthenticationOnHeader(..., configureOptions: options =>
            {
                // Adds shared access key authentication with emitting security events during the authentication of the request.
                // (default: `false`)
                options.EmitSecurityEvents = true
            }));
        }
    }
}
```

For this setup to work, an Arcus secret store is required as the provided secret name (in this case `"shared-access-key-name"`) will be looked up.
See [our offical documentation](https://security.arcus-azure.net/features/secret-store/) for more information about setting this up.

## Enforce shared access key authentication per controller or operation

### Introduction

The `SharedAccessKeyAuthenticationAttribute` can be added on both controller- and operation level in an <span>ASP.NET</span> Core application.
The shared access key authentication will then be applied to the endpoint(s) that are decorated with the `SharedAccessKeyAuthenticationAttribute`.

### Usage

We created an `SharedAccessKeyAuthenticationAttribute` attribute which can be applied on the controllers, or if more fine-grained control is needed, on the operations that requires authentication:

```csharp
using Arcus.WebApi.Security.Authentication.SharedAccessKey;

[ApiController]
[SharedAccessKeyAuthentication(headerName: "http-request-header-name", queryParameterName: "api-key", secretName: "shared-access-key-name")]
public class MyApiController : ControllerBase
{
    [HttpGet]
    [Route("authz/shared-access-key")]
    public IActionResult AuthorizedGet()
    {
        return Ok();
    }
}
```

For this setup to work, an Arcus secret store is required as the provided secret name (in this case `"shared-access-key-name"`) will be looked up.
See [our offical documentation](https://security.arcus-azure.net/features/secret-store/) for more information about setting this up.

#### Configuration

Some additional configuration options are available on the attribute.

```csharp
// Adds shared access key authentication with emitting of security events during the authentication of the request.
// (default: `false`)
[SharedAccessKeyAuthentication(..., EmitSecurityEvents: true)]
```

## Behavior in validating shared access key parameter
The package supports different scenarios for specifying the shared access key parameter and is supported for global or per controller/operation use cases.

- **Use header only** - Only the specified request header will be validated for the shared access key, any supplied query parameter will not be taken into account.

```csharp
using Arcus.WebApi.Security.Authentication.SharedAccessKey;
using Microsoft.Extensions.DependencyInjection;

public class Startup
{
    public void ConfigureServices(IServiceCollections services)
    {
        // See https://security.arcus-azure.net/features/secret-store/ for more information.
        services.AddSecretStore(stores =>  stores.AddAzureKeyVaultWithManagedIdentity("https://your-keyvault.vault.azure.net/", CacheConfiguration.Default));
        
        services.AddMvc(options => options.Filters.AddSharedAccessKeyAuthenticationOnHeader(headerName: "http-request-header-name", secretName: "shared-access-key-name")));
    }
}
```

- **Use query parameter only** - Only the specified query parameter  will be validated for the shared access key, any supplied request header will not be taken into account.

```csharp
using Arcus.WebApi.Security.Authentication.SharedAccessKey;
using Microsoft.Extensions.DependencyInjection;

public class Startup
{
    public void ConfigureServices(IServiceCollections services)
    {
        // See https://security.arcus-azure.net/features/secret-store/ for more information.
        services.AddSecretStore(stores =>  stores.AddAzureKeyVaultWithManagedIdentity("https://your-keyvault.vault.azure.net/", CacheConfiguration.Default));

        services.AddMvc(options => options.Filters.AddSharedAccessKeyAuthenticationOnQuery(queryParameterName: "api-key", secretName: "shared-access-key-name")));
    }
}
```

- **Support both header & query parameter** - Both the specified request header and query parameter  will be validated for the shared access key.

```csharp
using Arcus.WebApi.Security.Authentication.SharedAccessKey;
using Microsoft.Extensions.DependencyInjection;

public class Startup
{
    public void ConfigureServices(IServiceCollections services)
    {
        // See https://security.arcus-azure.net/features/secret-store/ for more information.
        services.AddSecretStore(stores =>  stores.AddAzureKeyVaultWithManagedIdentity("https://your-keyvault.vault.azure.net/", CacheConfiguration.Default));

        services.AddMvc(options => options.Filters.Add(
            new SharedAccessKeyAuthenticationFilter(
                headerName: "http-request-header-name", 
                queryParameterName: "api-key", 
                secretName: "shared-access-key-name")));
    }
}
```

If both header and query parameter are specified, they must both be valid or an `Unauthorized` will be returned.


## Bypassing authentication
The package supports a way to bypass the shared access key authentication for certain endpoints.
This works with adding one of these attributes to the respectively endpoint:
- `BypassSharedAccessKeyAuthentication`
- `AllowAnonymous`

> Works on both method and controller level, using either the shared access key filter or attribute.

```csharp
using Arcus.WebApi.Security.Authentication.SharedAccessKey;

[ApiController]
[SharedAccessKeyAuthentication("MySecret", "MyHeader")]
public class SystemController : ControllerBase
{
    [HttpGet('api/v1/health')]
    [BypassSharedAccessKeyAuthentication]
    public IActionResult GetHealth()
    {
        return Ok();
    }
}
```

[&larr; back](/)
