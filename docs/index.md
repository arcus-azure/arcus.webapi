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

- **Security**
    - [Shared access key authentication](features/auth/shared-access-key.md)
    - [Certificate authentication](features/auth/certificate.md)
- **Logging**
    - [Logging unhandled exceptions](features/logging.md)
- **OpenAPI**
    - [Exposing security information in Swashbuckle documentation](features/openapi/security-definitions.md)

# License
This is licensed under The MIT License (MIT). Which means that you can use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the web application. But you always need to state that Codit is the original author of this web application.

*[Full license here](https://github.com/arcus-azure/arcus.webapi/blob/master/LICENSE)*
