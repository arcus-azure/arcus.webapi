---
title: "JSON formatting"
layout: default
---

# JSON formatting
The `Arcus.WebApi.Hosting` library provides a way to restrict and configure the JSON input and output formatting of your application.
This allows for a easier and more secure formatting when working with JSON types.

## Installation
These features require to install our NuGet package:

```shell
PM > Install-Package Arcus.WebApi.Hosting
```

## Restricting JSON format
We have provided an extension that will allow you to restrict your input and output formatting to only JSON formatting. This means that all other incoming content will will result in `UnsupportedMediaType` failures and outcoming content will fail to serialize back to the sender. With this functionality, you'll be sure that you only have to deal with JSON.

Following example shows you where you can configure this:

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddMvc(mvcOptions => mvcOptions.OnlyAllowJsonFormatting());
    }
}
```

## Configure JSON format
We have provided an extension that will allow you to configure the input and output JSON formatting in one go. This means that any options you configure in this extension will automatically apply to the incoming model as well as the outgoing model. This makes the JSON formatting more streamlined and easier to maintain.

Following example shows you where you can configure these options:

```csharp
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddMvc(mvcOptions => mvcOptions.ConfigureJsonFormatting(jsonOptions =>
        {
            jsonOptions.IgnoreNullValues = true;
            jsonOptions.Converters.Add(new JsonStringEnumConverter());
        }));
    }
}