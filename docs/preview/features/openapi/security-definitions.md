---
title: "Adding security information to OpenAPI documentation"
layout: default
---

# Adding OAuth security definition to API operations

When an API is secured via OAuth, [Shared Access Key authentication](../../features/security/auth/shared-access-key), [Certificate authentication](../../features/security/auth/certificate), it is helpful if the Open API documentation makes this clear via a security scheme and the API operations that require authorization automatically inform the consumer that it is possible that a 401 Unauthorized or 403 Forbidden response is returned.

These `IOperationFilter`'s that are part of this package exposes this functionality:
- [`OAuthAuthorizeOperationFilter`](#oauth)
- [`SharedAccessKeyAuthenticationOperationFilter`](#sharedaccesskey)
- [`CertificateAuthenticationOperationFilter`](#certificate)

## Installation

This feature requires to install our NuGet package

```shell
PM > Install-Package Arcus.WebApi.OpenApi.Extensions
```

## Usage

### OAuth

To indicate that an API is protected by OAuth, you need to add `OAuthAuthorizeOperationFilter` as an `IOperationFilter` when configuring Swashbuckles Swagger generation:

```csharp
services.AddSwaggerGen(setupAction =>
{
    setupAction.SwaggerDoc("v1", new Info { Title = "My API v1", Version = "v1" });

    setupAction.AddSecurityDefinition("oauth2", new OAuth2Scheme
    {
        Flow = "implicit",
        AuthorizationUrl = $"{authorityUrl}connect/authorize",
        Scopes = scopes
    });
    
    setupAction.OperationFilter<OAuthAuthorizeOperationFilter>(new object[] { new[] { "myApiScope1", "myApiScope2" } });
});
```

### Shared Access Key

To indicate that an API is protected by [Shared Access Key authentication](../../features/security/auth/shared-access-key), you need to add `SharedAccessKeyAuthenticationOperationFilter` as an `IOperationFilter` when configuring Swashbuckles Swagger generation:

```csharp
services.AddSwaggerGen(setupAction =>
{
    setupAction.SwaggerDoc("v1", new Info { Title = "My API v1", Version = "v1" });

    setupAction.AddSecurityDefinition("sharedaccesskey", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey
     });

     setupAction.OperationFilter<SharedAccessKeyAuthenticationOperationFilter>(new object[] { new[] { "myApiScope1", "myApiScope2" } });
});
```

### Shared Access Key

To indicate that an API is protected by [Certificate authentication](../../features/security/auth/certificate), you need to add `CertificateAuthenticationOperationFilter` as an `IOperationFilter` when configuring Swashbuckles Swagger generation:

```csharp
services.AddSwaggerGen(setupAction =>
{
    setupAction.SwaggerDoc("v1", new Info { Title = "My API v1", Version = "v1" });

    setupAction.AddSecurityDefinition("certificate", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey
     });

     setupAction.OperationFilter<CertificateAuthenticationOperationFilter>(new object[] { new[] { "myApiScope1", "myApiScope2" } });
});
```

[&larr; back](/)
