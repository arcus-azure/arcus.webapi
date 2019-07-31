---
title: "Replace configuration tokens with ISecretProvider"
layout: default
---

The `Arcus.WebApi.Security` package provides a mechanism to use your own `ISecretProvider` implementation when building your configuration for your web application.

### Usage

When building your `IConfiguration`, you can call the extension `.AddAzureKeyVault` to pass in your version of the `ISecretProvider` instead on using a specific key vault store name of client.

```csharp
ISecretProvider yourSecretProvider = ...

var config = new ConfigurationBuilder()
    .AddAzureKeyVault(yourSecretProvider)
    .Build();

var host = new WebHostBuilder()
    .UseConfiguration(config)
    .UseKestrel()
    .UseStartup<Startup>();
```