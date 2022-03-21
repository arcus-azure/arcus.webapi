---
title: "Adding security information to OpenAPI documentation"
layout: default
---

# Adding OAuth security definition to API operations

When an API is secured via OAuth, it is helpful if the Open API documentation makes this clear via a security scheme and the API operations that require authentication/authorization automatically inform the consumer that it is possible that a 403 Forbidden or 401 Unauthorized response is returned.
The `OAuthAuthorizeOperationFilter` that is part of this package exposes this functionality.

## Installation

This feature requires to install our NuGet package

```shell
PM > Install-Package Arcus.WebApi.OpenApi.Extensions
```

## Usage

To indicate that an API is protected by OAuth, you need to add `AuthorizeCheckOperationFilter` as an `OperationFilter` when configuring Swashbuckles Swagger generation:

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

[&larr; back](/)
