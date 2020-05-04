---
title: "Telemetry Enrichment"
layout: default
---

# Telemetry Enrichment

![](https://img.shields.io/badge/Available%20starting-v0.4-green?link=https://github.com/arcus-azure/arcus.webapi/releases/tag/v0.4.0)

## Serilog Correlation Enrichment

The `Arcus.WebApi.Telemetry.Serilog` library provides a [Serilog enricher](https://github.com/serilog/serilog/wiki/Enrichment) 
that adds the correlation information of the current request to the log event as a log property called `TransactionId` and `OperationId`.

**Example**

- `TransactionId`: `A5E90591-ADB0-4A56-818A-AC5C02FBFF5F`
- `OperationId`: `79BB196A-B0CC-4F5C-B48A-AB87850346AF`

**Usage**

The enricher requires access to the application services so it can get the correlation information.
Following example shows how the Serilog logger is configured in the `Startup.cs` file.

```csharp
public class Startup
{
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        Log.Logger = new LoggerConfiguration()
            .Enrich.With(new CorrelationInfoEnricher(app.ApplicationServices))
            .CreateLogger();
    }
}
```

[&larr; back](/)