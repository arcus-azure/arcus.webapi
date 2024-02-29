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
We have provided an extension that will allow you to restrict your input and output formatting to only JSON formatting (Only the `SystemTextJsonInputFormatter` will remain). This means that all other incoming content will result in `UnsupportedMediaType` failures and outgoing content will fail to serialize back to the sender. With this functionality, you'll be sure that you only have to deal with JSON.

Following example shows you how you can configure this:

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;

WebApplicationBuilder builder = WebApplication.CreateBuilder();

builder.Services.AddControllers(mvcOptions =>
{
    mvcOptions.OnlyAllowJsonFormatting();
});
```
