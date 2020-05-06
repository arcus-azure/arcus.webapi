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

For more granular packages we recommend reading the documentation.

# Features

- **Security**
    - Authentication
        - [Shared access key authentication](features/security/auth/shared-access-key)
        - [Certificate authentication](features/security/auth/certificate)
- **Correlation**
    - [Provide request and/or transaction correlation ids](features/correlation)
- **Telemetry**
    - [Enrich with telemetry information](features/telemetry)
- **Logging**
    - [Logging unhandled exceptions](features/logging#logging-unhandled-exceptions)
- **OpenAPI**
    - [Exposing security information in Swashbuckle documentation](features/openapi/security-definitions)

## Older versions

- [v0.1](v0.1/index.md)
- [v0.2](v0.2/index.md)
- [v0.3](v0.3/index.md)
- [v0.4](v0.4/index.md)

# License
This is licensed under The MIT License (MIT). Which means that you can use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the web application. But you always need to state that Codit is the original author of this web application.

*[Full license here](https://github.com/arcus-azure/arcus.webapi/blob/master/LICENSE)*
