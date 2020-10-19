---
title: "Authorization with JWT via ASP.NET Core authorization filters"
layout: default
---

# Authorization with JWT

The `Arcus.WebApi.Security` package provides a mechanism that uses JWT (JSON Web Tokens) to authorize requests access to the web application.

This authorization process consists of the following parts:
1. Find the OpenID server endpoint to request the correct access token
2. Determine the request header name you want to use where the access token should be available

- [Globally enforce JWT authorization](#globally-enforce-jwt-authorization)
- [Bypassing authorization](#bypassing-authorization)

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
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Default configuration:
        // By default, the JWT authorization filter will use the Microsoft 'https://login.microsoftonline.com/common/v2.0/.well-known/openid-configuration' OpenID endpoint to request the configuration.
        services.AddMvc(mvcOptions => mvcOptions.Filters.AddJwtTokenAuthorization());

        // Default configuration with validation parameters:
        // One can still use the default Microsoft OpenID endpoint and provide additional validation parameters to manipulate how the JWT token should be validated.
        services.AddMvc(mvcOptions => 
        {
            var parameters = new TokenValidationParameters();
            mvcOptions.Filters.AddJwtTokenAuthorization(options => options.JwtTokenReader = new JwtTokenReader(parameters));
        });

        // Default configuration with application ID:
        // One can use the Microsoft OpenID endpoint and provide just the application ID as input for the validation parameters. 
        // By default will only the issuer singing keys and lifetime be validated.
        services.AddMvc(mvcOptions => 
        {
            string applicationId = "e98s9-sadf8981-asd8f79-ahtew8";
            mvcOptions.Filters.AddJwtTokenAuthorization(options => options.JwtTokenReader = new JwtTokenReader(applicationId));
        });

        // Custom OpenID endpoint:
        // You can use your own custom OpenID endpoint by providing another the endpoint in the options; additionally with custom validation parameters how the JWT token should be validated.
        services.AddMvc(mvcOptions => 
        {
            var parameters = new TokenValidationParameters();
            string endpoint = "https://localhost:5000/.well-known/openid-configuration";
            mvcOptions.Filters.AddJwtTokenAuthorization(options => options.JwtTokenReader = new JwtTokenReader(parameters, endpoint));
        });
    }
}
```

## Bypassing authentication

The package supports a way to bypass the JWT authorization for certain endponts.
This works with adding one of these attributes to the respectively endpoint:
- `BypassJwtAuthorization`
- `AllowAnonymous`

> Works on both method and controller level.

```csharp
[ApiController]
public class MyController : ControllerBase
{
    [HttpGet]
    [BypassJwtAuthorization]
    public IActionResult Get()
    {
        return Ok();
    }
}
```
