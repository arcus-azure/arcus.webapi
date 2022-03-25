---
title: "Authorization with JWT via ASP.NET Core authorization filters"
layout: default
---

# Authorization with JWT

The `Arcus.WebApi.Security` package provides a mechanism that uses JWT (JSON Web Tokens) to authorize requests access to the web application.

This authorization process consists of the following parts:
1. Find the OpenID server endpoint to request the correct access token
2. Determine the request header name you want to use where the access token should be available

- [Authorization with JWT](#authorization-with-jwt)
  - [Globally enforce JWT authorization](#globally-enforce-jwt-authorization)
    - [Installation](#installation)
    - [Usage](#usage)
    - [Custom Claim validation](#custom-claim-validation)
  - [Bypassing authentication](#bypassing-authentication)
  - [Accessing secret store on JWT Bearer token authentication](#accessing-secret-store-on-jwt-bearer-token-authentication)

## Globally enforce JWT authorization

### Installation

This feature requires to install our NuGet package:

```shell
PM > Install-Package Arcus.WebApi.Security
```

### Usage

The `JwtTokenAuthorizationFilter` can be added to the request filters in an <span>ASP.NET</span> Core application.
This filter will then add authorization to all endpoints via the configured properties on the filter itself.

Example:

```csharp
using Arcus.WebApi.Security.Authorization.Jwt;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddMvc(mvcOptions =>
        {
            // Default configuration:
            // By default, the JWT authorization filter will use the Microsoft 'https://login.microsoftonline.com/common/v2.0/.well-known/openid-configuration' OpenID endpoint to request the configuration.
            mvcOptions.Filters.AddJwtTokenAuthorization();

            mvcOptions.Filters.AddJwtTokenAuthorization(options =>
            {
                // Default configuration with validation parameters:
                // One can still use the default Microsoft OpenID endpoint and provide additional validation parameters to manipulate how the JWT token should be validated.
                var parameters = new TokenValidationParameters();
                options.JwtTokenReader = new JwtTokenReader(parameters);

                // Default configuration with application ID:
                // One can use the Microsoft OpenID endpoint and provide just the application ID as input for the validation parameters. 
                // By default will only the issuer singing keys and lifetime be validated.
                string applicationId = "e98s9-sadf8981-asd8f79-ahtew8";
                options.JwtTokenReader = new JwtTokenReader(applicationId);

                // Custom OpenID endpoint:
                // You can use your own custom OpenID endpoint by providing another the endpoint in the options; additionally with custom validation parameters how the JWT token should be validated.
                var parameters = new TokenValidationParameters();
                string endpoint = "https://localhost:5000/.well-known/openid-configuration";
                options.JwtTokenReader = new JwtTokenReader(parameters, endpoint);

                // Emitting security events:
                // One can opt-in for security events during the authorization of the request (default: `false`).
                options.EmitSecurityEvents = true;
            });
        });
    }
}
```

### Custom Claim validation

It allows validating not only on the audience claim in the JWT token, but any type of custom claim that needs to be verified

```csharp
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Default configuration with issuer:
        // One can use the Microsoft OpenID endpoint and provide just the issuer as input for the validation parameters.         
        services.AddMvc(mvcOptions => 
        {
            var claimCheck = new Dictionary<string, string>
            {
                {"aud", Issuer}
            };
            mvcOptions.Filters.AddJwtTokenAuthorization(claimCheck);
        });

        // Custom OpenID endpoint:
        // You can use your own custom OpenID endpoint by providing another the endpoint in the options; additionally with custom validation parameters and custom claims to manipulate how the JWT token should be validated.
        services.AddMvc(mvcOptions => 
        {
            var claimCheck = new Dictionary<string, string>
            {
                {"aud", Issuer},
                {"oid", "fa323e12-e4b8-4e22-bb2a-b18cb4b76301"}
            };
            mvcOptions.Filters.AddJwtTokenAuthorization(claimCheck);
        });

        // Default configuration with validation parameters:
        // One can still use the default Microsoft OpenID endpoint and provide additional validation parameters and custom claims to manipulate how the JWT token should be validated.
        services.AddMvc(mvcOptions => 
        {
            var parameters = new TokenValidationParameters();

            var claimCheck = new Dictionary<string, string>
            {
                {"aud", Issuer},
                {"oid", "fa323e12-e4b8-4e22-bb2a-b18cb4b76301"}
            };

            mvcOptions.Filters.AddJwtTokenAuthorization(claimCheck);
        });

    }
}
```

## Bypassing authentication

The package supports a way to bypass the JWT authorization for certain endpoints.
This works with adding one of these attributes to the respectively endpoint:
- `BypassJwtAuthorization`
- `AllowAnonymous`

> Works on both method and controller level.

```csharp
using Arcus.WebApi.Security.Authorization.Jwt;

[ApiController]
public class SystemController : ControllerBase
{
    [HttpGet('api/v1/health')]
    [BypassJwtAuthorization]
    public IActionResult GetHealth()
    {
        return Ok();
    }
}
```

## Accessing secret store on JWT Bearer token authentication

This package also provides an extra extension to access the [Arcus secret store](https://security.arcus-azure.net/features/secret-store/) while configuring JWT Bearer token authentication.
Access to the secret store will help to provide, for example, issuer symmetric keys.

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddAuthentication(...)
            .AddJwtBearer((options, serviceProvider) =>
            {
                var secretProvider = serviceProvider.GetRequiredService<ISecretProvider>();
                string key = secretProvider.GetRawSecretAsync("JwtSigningKey").GetAwaiter().GetResult();

                options.TokenValidationParameters = new TokenValidationParameters
                {
                   IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                };
            })
}
```