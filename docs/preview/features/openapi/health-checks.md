---
title: "Using health checks report in OpenAPI documentation"
layout: default
---

# Using health checks report in OpenAPI documentation

Microsoft has a way of providing the health status of an application by running over a series of 'health checks' that eventually generate a health checks report. 
For more information on application health, see [Microsoft's documentation](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks).

The problem is that the health checks report model called `HealthReport` cannot be safely used as a JSON serializable type, as it could contain exception details (and therefore assembly information).
Arcus provides a dedicated JSON data-trasfer object (DTO) to expose the health status of the application in a reliable and safe manner.

## Installation

This feature requires to install our NuGet package

```shell
PM > Install-Package Arcus.WebApi.OpenApi.Extensions
```

## Usage

The Arcus-provided health checks status model is called `HealthReportJson` and has the same information as Microsoft's model but without any exception details.
The Arcus model and Microsoft's model are perfectly exchangable. They can be converted between each other, but keep in mind that the exception details are not part of this conversion.

Here's an example of an API controller that uses the Microsoft's health checks service to generate the health report, but uses Arcus' model to expose this information.
Notice that the `[ProduceResponseType]` attribute uses Arcus' model so that the OpenAPI document uses this model during its generation.

```csharp
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

[ApiController]
[Route("api/v1/health")]
public class HealthController : ControllerBase
{
    private readonly HealthCheckService _healthService;

    public HealthController(HealthCheckService healthService)
    {
        _healthService = healthService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(HealthReportJson), 200)]
    public async Task<IActionResult> Get()
    {
        HealthReport report = await _healthService.CheckHealthAsync();

        var json = HealthReportJson.FromHealthReport(report);
        return Ok(json);
    }
}
```

> 💡 The `HealthReportJson` has implicit operators that does the conversion between Microsoft's and Arcus health checks report for you, so you can use `HealthReportJson json = await _healthService.CheckHealthAsync()` and the conversion happens behind the screens.