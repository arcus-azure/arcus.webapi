---
title: "Adding security information to OpenAPI documentation"
layout: default
---

## Adding OAuth security definition to API operations

### Introduction

When an API is secured via OAuth, it is helpful if the Open API documentation makes this clear via a security scheme and the API operations that require authorization automatically inform the consumer that it is possible that a 401 Unauthorized or 403 Forbidden response is returned.
The `OAuthAuthorizeOperationFilter` that is part of this package exposes this functionality.

### Usage

To indicate that an API is protected by OAuth, you need to add `AuthorizeCheckOperationFilter` as an `OperationFilter` when configuring Swashbuckles Swagger generation:

```csharp
services.AddSwaggerGen(c =>
{
   c.SwaggerDoc("v1", new Info { Title = "My API v1", Version = "v1" });

   c.AddSecurityDefinition("oauth2", new OAuth2Scheme
   {
      Flow = "implicit",
      AuthorizationUrl = $"{authorityUrl}connect/authorize",
      Scopes = scopes
   });

   c.OperationFilter<OAuthAuthorizeOperationFilter>("myApiScope1", "myApiScope2");
});
```
