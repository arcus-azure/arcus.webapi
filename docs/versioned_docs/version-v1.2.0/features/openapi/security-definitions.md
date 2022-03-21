---
title: "Adding security information to OpenAPI documentation"
layout: default
---

# Adding OAuth security definition to API operations

When an API is secured via OAuth, [Shared Access Key authentication](../../features/security/auth/shared-access-key), [Certificate authentication](../../features/security/auth/certificate), it is helpful if the Open API documentation makes this clear via a security scheme and the API operations that require authentication/authorization automatically inform the consumer that it is possible that a 403 Forbidden or 401 Unauthorized response is returned.

These `IOperationFilter`'s that are part of this package exposes this functionality:
- [Adding OAuth security definition to API operations](#adding-oauth-security-definition-to-api-operations)
  - [Installation](#installation)
  - [Usage](#usage)
    - [Certificate](#certificate)
    - [OAuth](#oauth)
    - [Shared Access Key](#shared-access-key)

## Installation

This feature requires to install our NuGet package

```shell
PM > Install-Package Arcus.WebApi.OpenApi.Extensions
```

## Usage

### Certificate

To indicate that an API is protected by [Certificate authentication](../../features/security/auth/certificate), you need to add `CertificateAuthenticationOperationFilter` as an `IOperationFilter` when configuring Swashbuckles Swagger generation:

```csharp
using Arcus.WebApi.OpenApi.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSwaggerGen(setupAction =>
        {
            setupAction.SwaggerDoc("v1", new OpenApiInfo { Title = "My API v1", Version = "v1" });

            string securitySchemaName = "my-certificate";
            setupAction.AddSecurityDefinition(securitySchemaName, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.ApiKey
             });

             setupAction.OperationFilter<CertificateAuthenticationOperationFilter>(securitySchemaName);
        });
    }
}
```

> Note: the `CertificateAuthenticationOperationFilter` has by default `"certificate"` as `securitySchemaName`.

### OAuth

To indicate that an API is protected by OAuth, you need to add `OAuthAuthorizeOperationFilter` as an `IOperationFilter` when configuring Swashbuckles Swagger generation:

```csharp
using Arcus.WebApi.OpenApi.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSwaggerGen(setupAction =>
        {
            setupAction.SwaggerDoc("v1", new Info { Title = "My API v1", Version = "v1" });

            string securitySchemaName = "my-oauth2";
            setupAction.AddSecurityDefinition(securitySchemaName, new OAuth2Scheme
            {
                Flow = "implicit",
                AuthorizationUrl = $"{authorityUrl}connect/authorize",
                Scopes = scopes
            });

            setupAction.OperationFilter<OAuthAuthorizeOperationFilter>(securitySchemaName, new object[] { new[] { "myApiScope1", "myApiScope2" } });
        });
    }
}
```

> Note: the `OAuthAuthorizeOperationFilter` has by default `"oauth2"` as `securitySchemaName`.

### Shared Access Key

To indicate that an API is protected by [Shared Access Key authentication](../../features/security/auth/shared-access-key), you need to add `SharedAccessKeyAuthenticationOperationFilter` as an `IOperationFilter` when configuring Swashbuckles Swagger generation:

```csharp
using Arcus.WebApi.OpenApi.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSwaggerGen(setupAction =>
        {
            setupAction.SwaggerDoc("v1", new new OpenApiInfo { Title = "My API v1", Version = "v1" });

            string securitySchemaName = "my-sharedaccesskey";
            setupAction.AddSecurityDefinition(securitySchemaName, new OpenApiSecurityScheme
            {
                Name = "X-API-Key",
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header
             });

             setupAction.OperationFilter<SharedAccessKeyAuthenticationOperationFilter>(securitySchemaName);
        });
    }
}
```

> Note: the `SharedAccessKeyAuthenticationOperationFilter` has by default `"sharedaccesskey"` as `securitySchemaName`.

[&larr; back](/)
