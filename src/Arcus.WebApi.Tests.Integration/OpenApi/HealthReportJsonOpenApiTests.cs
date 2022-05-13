using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.Testing.Logging;
using Arcus.WebApi.Tests.Integration.Controllers;
using Arcus.WebApi.Tests.Integration.Fixture;
using Arcus.WebApi.Tests.Integration.OpenApi.Controllers;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Arcus.WebApi.Tests.Integration.OpenApi
{
    [Collection("Integration")]
    public class HealthReportJsonOpenApiTests
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="HealthReportJsonOpenApiTests" /> class.
        /// </summary>
        public HealthReportJsonOpenApiTests(ITestOutputHelper outputWriter)
        {
            _logger = new XunitTestLogger(outputWriter);
        }

        [Fact]
        public async Task Route_WithHealthReportJsonResponse_ShouldCorrectlySerializeReport()
        {
            string assemblyName = typeof(AuthenticationOperationFilterTests).Assembly.GetName().Name;
            var options = new TestApiServerOptions()
                .ConfigureServices(services =>
                {
                    services.AddMvc()
                            .AddNewtonsoftJson();

                    services.AddHealthChecks()
                            .AddCheck("sample", () => HealthCheckResult.Unhealthy("sample description", new InvalidOperationException("Something happened!")));

                    var openApiInformation = new OpenApiInfo { Title = assemblyName, Version = "v1" };
                    services.AddSwaggerGen(swagger =>
                    {
                        swagger.SwaggerDoc("v1", openApiInformation);
                        swagger.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, assemblyName + ".Open-Api.xml"));
                    });
                })
                .Configure(app =>
                {
                    app.UseSwagger();
                    app.UseSwaggerUI(swagger =>
                    {
                        swagger.SwaggerEndpoint("v1/swagger.json", assemblyName);
                        swagger.DocumentTitle = assemblyName;
                    });
                });

            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                // Assert
                await AssertHealthReportSerializationAsync(server);
                await AssertSwaggerDocumentContainsHealthReportJsonAsync(server);
            }
        }

        private static async Task AssertHealthReportSerializationAsync(TestApiServer server)
        {
            var request = HttpRequestBuilder.Get(HealthController.GetRoute);
            using (HttpResponseMessage response = await server.SendAsync(request))
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                string json = await response.Content.ReadAsStringAsync();

                HealthReport report = JsonConvert.DeserializeObject<HealthReportJson>(json);
                Assert.NotNull(report);
                Assert.Equal(HealthStatus.Unhealthy, report.Status);
                (string entryName, HealthReportEntry entry) = Assert.Single(report.Entries);
                Assert.NotEmpty(entryName);
                Assert.True(entry.Duration > TimeSpan.Zero, "entry.Duration > Zero");
                Assert.NotNull(entry.Description);
                Assert.Null(entry.Exception);
            }
        }

        private async Task AssertSwaggerDocumentContainsHealthReportJsonAsync(TestApiServer server)
        {
            var request = HttpRequestBuilder.Get("/swagger/v1/swagger.json");
            using (HttpResponseMessage response = await server.SendAsync(request))
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                OpenApiDocument swagger = await ReadSwaggerOpenAPiDocumentAsync(response);
                Assert.True(swagger.Paths.TryGetValue("/" + HealthController.GetRoute, out OpenApiPathItem path),
                    $"Cannot find /{DefaultController.GetRoute} path in Open API spec file");

                (OperationType _, OpenApiOperation value) = Assert.Single(path.Operations);
                (string _, OpenApiResponse resp) = Assert.Single(value.Responses);
                (string _, OpenApiMediaType applicationJsonType) =
                    Assert.Single(resp.Content, content => content.Key == "application/json");

                Assert.Equal(nameof(HealthReportJson), applicationJsonType.Schema.Reference.Id);
                Assert.Contains(swagger.Components.Schemas, schema => schema.Key == nameof(HealthReportJson));
            }
        }

        private async Task<OpenApiDocument> ReadSwaggerOpenAPiDocumentAsync(HttpResponseMessage response)
        {
            var reader = new OpenApiStreamReader();
            using (var responseStream = await response.Content.ReadAsStreamAsync())
            {
                OpenApiDocument swagger = reader.Read(responseStream, out OpenApiDiagnostic diagnostic);
                _logger.LogTrace(diagnostic.Errors.Count == 0
                    ? String.Empty
                    : String.Join(", ", diagnostic.Errors.Select(e => e.Message + ": " + e.Pointer)));

                return swagger;
            }
        }
    }
}
