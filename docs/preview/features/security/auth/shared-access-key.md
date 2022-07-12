---
title: "Authentication with shared access keys via ASP.NET Core authentication filters"
layout: default
---

# Authentication with shared access keys

The `Arcus.WebApi.Security` package provides a mechanism that uses shared access keys to grant access to a web application.
This authentication process consists of two parts:

1. Find the configured parameter that holds the shared access key, this can be a request header, a query parameter or both.
2. Shared access key matches the value with the secret stored, determined via configured secret provider

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
using Arcus.Security.Core.Caching.Configuration;
using Microsoft.AspNetCore.Builder;

WebApplicationBuilder builder = WebApplication.CreateBuilder();

builder.Services.AddSecretStore(stores =>
{
    stores.AddAzureKeyVaultWithManagedIdentity("https://your-keyvault.vault.azure.net/", CacheConfiguration.Default));
});

builder.Services.AddControllers(options =>
{
    // Adds shared access key authentication to the request pipeline where the request query string parameter will be verified 
    // if the query parameter value contain the expected secret value, retrievable with the given secret name.
    options.AddSharedAccessKeyAuthenticationFilterOnQuery(
        queryParameterName: "api-key", 
        secretName: "shared-access-key-name")));

    // Adds shared access key authentication to the request pipeline where only the request header will be verified if it contains the expected secret value.
    options.AddSharedAccessKeyAuthenticationFilterOnHeader(
        headerName: "http-request-header-name",
        secretName: "shared-access-key-name"));

    // Additional consumer-configurable options to change the behavior of the authentication filter.
    options.AddSharedAccessKeyAuthenticationFilterOnHeader(..., configureOptions: opt =>
    {
        // Adds shared access key authentication with emitting security events during the authentication of the request.
        // (default: `false`)
        opt.EmitSecurityEvents = true
    }));
});
```

For this setup to work, an Arcus secret store is required as the provided secret name (in this case `"shared-access-key-name"`) will be looked up.
See [our official documentation](https://security.arcus-azure.net/features/secret-store/) for more information about setting this up.

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
See [our official documentation](https://security.arcus-azure.net/features/secret-store/) for more information about setting this up.

#### Configuration

Some additional configuration options are available on the attribute.

```csharp
// Adds shared access key authentication with emitting of security events during the authentication of the request.
// (default: `false`)
[SharedAccessKeyAuthentication(..., EmitSecurityEvents: true)]
```

## Behavior in validating shared access key parameter
The package supports different scenarios for specifying the shared access key parameter and is supported for global or per controller/operation use cases.

- **Use header** - Only the specified request header will be validated for the shared access key, any supplied query parameter will not be taken into account.

```csharp
using Arcus.Security.Core.Caching.Configuration;
using Microsoft.AspNetCore.Builder;

WebApplicationBuilder builder = WebApplication.CreateBuilder();

// See https://security.arcus-azure.net/features/secret-store/ for more information.
builder.Services.AddSecretStore(stores => 
{
    stores.AddAzureKeyVaultWithManagedIdentity("https://your-keyvault.vault.azure.net/", CacheConfiguration.Default));
});

builder.Services.AddControllers(options =>
{
    options.AddSharedAccessKeyAuthenticationFilterOnHeader(headerName: "http-request-header-name", secretName: "shared-access-key-name"));
});
```

- **Use query parameter** - Only the specified query parameter  will be validated for the shared access key, any supplied request header will not be taken into account.

```csharp
using Arcus.Security.Core.Caching.Configuration;
using Microsoft.AspNetCore.Builder;

WebApplicationBuilder builder = WebApplication.CreateBuilder();

// See https://security.arcus-azure.net/features/secret-store/ for more information.
builder.Services.AddSecretStore(stores => 
{
    stores.AddAzureKeyVaultWithManagedIdentity("https://your-keyvault.vault.azure.net/", CacheConfiguration.Default));
});

builder.Services.AddControllers(options =>
{
    options.AddSharedAccessKeyAuthenticationFilterOnQuery(queryParameterName: "api-key", secretName: "shared-access-key-name"));
});
```

## Bypassing authentication
The package supports a way to bypass the shared access key authentication for certain endpoint.
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