# Shared access key authentication with Azure Key Vault integration
The Arcus shared access key authentication is an API security filter to easily and secure share the same access key across a set of API endpoints. This kind of authentication is very common and therefore a popular feature in the Arcus WebApi library.

The API security filter makes use of the [Arcus secret store](https://security.arcus-azure.net/features/secret-store) to retrieve the access keys. This makes sure that the secrets are safely accessed in the application.

This user guide will walk through the process of adding shared access key authentication to an existing Web API application, using Azure Key Vault as location to store the access keys.

## Terminology
To fully understand this user guide, some terminology is required:
* **Shared access key**: a single secret that's being used as the authentication mechanism of many API endpoints. Using a single secret means that there's also a single authorization level.
* **Arcus secret store**: the secret store is a concept in the Arcus Security library to centralize secrets in an application ([more info](https://security.arcus-azure.net/features/secret-store)).

## Sample application
In this user guide, a fictive API application will be used to add the shared access key authentication to. We will be working with two major parts.

The initial place where the application will be started:
```csharp
public class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        builder.Services.AddRouting();
        builder.Services.AddControllers();

        WebApplication app = builder.Build();
        app.UseRouting();
        app.Run();
    }
}
```

And the API controller that should be secured:
```csharp
[ApiController]
[Route("api/v1/order")]
public class OrderController : ControllerBase
{
    [HttpPost]
    public IActionResult Post([FromBody] Order order)
    {
        // Process order...
        return Accepted();
    }
}
```

## 1. Installation
To make use of the shared access key authentication, storing its secrets in Azure Key Vault, following Arcus packages have to be installed.
```shell
PM > Install-Package -Name Arcus.WebApi.Security
PM > Install-Package -Name Arcus.Security.Providers.AzureKeyVault
```

## 2. Use Arcus secret store with Azure Key Vault integration
Once the packages are installed, add the secret store via extensions to the API application: 
* 2.1 Use the `.ConfigureSecretStore` to setup the secret store with necessary secret providers 
* 2.2 Use the the `.AddAzureKeyVaultWithManagedIdentity` to add the Azure Key Vault secret provider to the secret store

```csharp
using Arcus.Security.Core.Caching.Configuration;

public class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        builder.AddRouting();
        builder.AddControllers();

        builder.Host.ConfigureSecretStore((config, stores) =>
        {
            stores.AddAzureKeyVaultWithManagedIdentity("https://your-key.vault.azure.net", CacheConfiguration.Default);
        });

        WebApplication app = builder.Build();
        app.UseRouting();
        app.Run();
    }
}
```

## 3. Use Arcus shared access key authentication API filter
This user guide will make use of the recommended way of securing API endpoints. This is done by registering the authentication mechanism in the startup code and on the API endpoint itself. That being said, we do support finer-grained authentication. See [our dedicated feature documentation](https://webapi.arcus-azure.net/features/security/auth/shared-access-key) for more information.

The `Arcus.WebApi.Security` provides with a single package all the available authentication and authorization security mechanisms. For shared access key authentication, we will be using the `AddSharedAccessKeyAuthenticationFilterOnHeader` extension which will register a global API authentication security filter that applies on all available API endpoints:
```csharp
using Arcus.Security.Core.Caching.Configuration;

public class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        builder.AddRouting();
        builder.AddControllers(options =>
        {
            options.AddSharedAccessKeyAuthenticationFilterOnHeader(
                "X-API-Key",
                "MyAccessKey_SecretName_AvailableInSecretStore");
        });

        builder.Host.ConfigureSecretStore((config, stores) =>
        {
            stores.AddAzureKeyVaultWithManagedIdentity("https://your-key.vault.azure.net", CacheConfiguration.Default);
        });

        WebApplication app = builder.Build();
        app.UseRouting();
        app.UseEndpoints(endpoints => endpoints.MapControllers());
        app.Run();
    }
}
```

* The `X-API-Key` is the HTTP request header name where the shared access key should be located. Missing or invalid header values will result in `401 Unauthorized` HTTP responses.
* The `MyAccessKey_SecretName_AvailableInSecretStore` is the name of the secret that holds the shared access key in the Arcus secret store. The secret store will try to find the secret by this name in one of its secret providers, in this case Azure Key Vault.

The shared access key API authentication filter will then try to match the found access key secret with the incoming access key secret, located in the HTTP request header. Successful matches will delegate the request further up the application, unsuccessful matches will result in unauthorized results:
```powershell
$headers = @{
    'Content-Type'='application/json'
    'X-API-Key'='invalid-key'
}

curl -Method POST `
  -Headers $headers  `
  'http://localhost:787/api/v1/order' `
  -Body '{ "OrderId": "3", "ProductName": "Fancy desk" }'

# Content: Shared access key in request doesn't match expected access key
# StatusCode : 401

$headers = @{
    'Content-Type'='application/json'
    'X-API-Key'='valid-key'
}

curl -Method POST `
  -Headers $headers  `
  'http://localhost:787/api/v1/order' `
  -Body '{ "OrderId": "3", "ProductName": "Fancy desk" }'

# StatusCode : 202
```

## Conclusion
In this user guide, you've seen how the Arcus shared access key API authentication filter can be added to an existing application, using the Arcus secret store that places the access in Azure Key Vault.

Besides shared access key authentication, we support several other mechanisms and useful API functionality. See our [feature documentation](https://webapi.arcus-azure.net/) for more information.

## Further reading
* [Arcus Web API documentation](https://webapi.arcus-azure.net/)
  * [Shared access key authentication](https://webapi.arcus-azure.net/features/security/auth/shared-access-key)
* [Arcus Security secret store documentation](https://security.arcus-azure.net/features/secret-store)
  * [Azure Key Vault integration](https://security.arcus-azure.net/features/secret-store/provider/key-vault)
* [Arcus Web API project template](https://templates.arcus-azure.net/features/web-api-template)
* [Integrating Arcus API Security Filters within F# Giraffe Function Pipelines](https://www.codit.eu/blog/arcus-api-security-filters-giraffe-function-pipelines/)
* [Out-of-the-box Request Tracking, Simplified HTTP Correlation & JWT Authorization in Arcus Web API v1.0](https://www.codit.eu/blog/out-of-the-box-request-tracking-simplified-http-correlation-jwt-authorization-in-arcus-web-api-v1-0/)