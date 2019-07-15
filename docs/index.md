---
title: "Home"
layout: default
permalink: /
redirect_from:
 - /index.html
---

[![NuGet Badge](https://buildstats.info/nuget/Arcus.WebApi.All?includePreReleases=true)](https://www.nuget.org/packages/Arcus.WebApi.All/)

# Installation

The Arcus.WebApi library can be installed via NuGet.

To install all Arcus.WebApi packages:

```shell
PM > Install-Package Arcus.WebApi.All
```

To install the Arcus.WebApi.Logging package:

```shell
PM > Install-Package Arcus.WebApi.Logging
```

To install the Arcus.WebApi.OpenApi.Extensions package:

```shell
PM > Install-Package Arcus.WebApi.OpenApi.Extensions
```

# Features

## Arcus.WebApi.Security

The `Arcus.WebApi.Security` package contains functionality to easily add security capabilities to an API.

- [Shared access key authentication](features/shared-access-key.md)

## Arcus.WebApi.Logging

The `Arcus.WebApi.Logging` package contains functionality that can be incorporated in API projects to easily add logging capabilities to an API project.

- [Logging unhandled exceptions](features/logging.md)

## OpenAPI Extensions

The `Arcus.WebApi.OpenApi.Extensions` package contains functionality that can be used to easily improve the Open API documentation of an API when making using of Swashbuckle.

- [Exposing security information in Swashbuckle documentation](features/openapi.securitydefinitions.md)

# License
This is licensed under The MIT License (MIT). Which means that you can use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the web application. But you always need to state that Codit is the original author of this web application.

*[Full license here](https://github.com/arcus-azure/arcus.webapi/blob/master/LICENSE)*
