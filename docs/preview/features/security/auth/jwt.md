---
title: "Authorization with JWT via ASP.NET Core authorization filters"
layout: default
---

# Authorization with JWT

The `Arcus.WebApi.Security` package provides a mechanism that uses JWT (JSON Web Tokens) to authorize requests access to the web application.

This authorization process consists of the following parts:
1. Find the OpenID server endpoint to request the correct access token
2. Determine the request header name you want to use where the access token should be available

## Installation

This feature requires to install our NuGet package:

```shell
PM > Install-Package Arcus.WebApi.Security
```

## Usage

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

### Custom Claim validation

It allows validating not only on the audience claim in the JWT token, but any type of custom claim that needs to be verified

```csharp
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