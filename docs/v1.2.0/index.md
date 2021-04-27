---
title: "Home"
layout: default
---

[![NuGet Badge](https://buildstats.info/nuget/Arcus.WebApi.All?packageVersion=1.2.0)](https://www.nuget.org/packages/Arcus.WebApi.All/1.2.0)

# Installation

The Arcus.WebApi library can be installed via NuGet.

To install all Arcus.WebApi packages:

```shell
PM > Install-Package Arcus.WebApi.All --Version 1.2.0
```

For more granular packages we recommend reading the documentation.

# Features

- **Security**
    - Authentication
        - [Shared access key authentication](features/security/auth/shared-access-key)
        - [Certificate authentication](features/security/auth/certificate)
    - Authorization
        - [JWT authorization](features/security/auth/jwt)
- **Correlation**
    - [Provide request and/or transaction correlation ids](features/correlation)
- **Telemetry**
    - [Enrich with telemetry information](features/telemetry)
- **Logging**
    - [Logging unhandled exceptions](features/logging#logging-unhandled-exceptions)
    - [Logging incoming requests](features/logging#logging-incoming-requests)
- **OpenAPI**
    - [Exposing security information in Swashbuckle documentation](features/openapi/security-definitions)

# License
This is licensed under The MIT License (MIT). Which means that you can use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the web application. But you always need to state that Codit is the original author of this web application.

*[Full license here](https://github.com/arcus-azure/arcus.webapi/blob/master/LICENSE)*

# Older versions

- [v1.1.0](v1.1.0)
- [v1.0.0](v1.0.0)
- [v0.4.0](v0.4.0)
- [v0.3.0](v0.3.0)
- [v0.2.0](v0.2.0)
- [v0.1.0](v0.1.0)
