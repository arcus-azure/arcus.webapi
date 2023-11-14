using System.Net;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Arcus.Testing.Logging;
using Arcus.WebApi.Tests.Integration.Fixture;
using Arcus.WebApi.Tests.Integration.Hosting.Formatting.Controllers;
using Arcus.WebApi.Tests.Integration.Hosting.Formatting.Fixture;
using Bogus;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Arcus.WebApi.Tests.Integration.Hosting.Formatting
{
    [Collection(Constants.TestCollections.Integration)]
    [Trait(Constants.TestTraits.Category, Constants.TestTraits.Integration)]
    public class MvcOptionsExtensionsTests
    {
        private readonly ILogger _logger;

        private static readonly Faker BogusGenerator = new Faker();

        /// <summary>
        /// Initializes a new instance of the <see cref="MvcOptionsExtensionsTests" /> class.
        /// </summary>
        public MvcOptionsExtensionsTests(ITestOutputHelper outputWriter)
        {
            _logger = new XunitTestLogger(outputWriter);
        }

        [Fact]
        public async Task IncomingText_WithoutOnlyAllowJsonFormatting_Succeeds()
        {
            // Arrange
            var options = new TestApiServerOptions()
                .ConfigureServices(services => services.AddMvc(opt =>
                {
                    opt.InputFormatters.Add(new PlainTextInputFormatter());
                }));

            string sentence = string.Join(" ", BogusGenerator.Lorem.Words());
            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder
                    .Get(CountryController.GetPlainTextRoute)
                    .WithTextBody(sentence);
                
                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    Assert.Equal("text/plain", response.Content.Headers.ContentType.MediaType);
                    string actual = await response.Content.ReadAsStringAsync();
                    Assert.Equal(sentence, actual);
                }
            }
        }

        [Fact]
        public async Task IncomingText_WithOnlyAllowJsonFormatting_Fails()
        {
            // Arrange
            var options = new TestApiServerOptions()
                .ConfigureServices(services => services.AddMvc(opt =>
                {
                    opt.InputFormatters.Add(new PlainTextInputFormatter());
                    opt.OnlyAllowJsonFormatting();
                }));

            string sentence = string.Join(" ", BogusGenerator.Lorem.Words());
            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder
                    .Get(CountryController.GetPlainTextRoute)
                    .WithTextBody(sentence);
                
                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
                }
            }
        }

        [Fact]
        public async Task IncomingJson_WithOnlyAllowJsonFormatting_Succeeds()
        {
            // Arrange
            var options = new TestApiServerOptions()
                .ConfigureServices(services => services.AddMvc(opt =>
                {
                    opt.InputFormatters.Add(new PlainTextInputFormatter());
                    opt.OnlyAllowJsonFormatting();
                }));
            
            var country = new Country
            {
                Name = BogusGenerator.Address.Country(),
                Code = BogusGenerator.Random.Enum<CountryCode>()
            };

            string json = JsonSerializer.Serialize(country);
            await using (var server = await TestApiServer.StartNewAsync(options, _logger))
            {
                var request = HttpRequestBuilder
                    .Get(CountryController.GetJsonRoute)
                    .WithJsonBody(json);
                
                // Act
                using (HttpResponseMessage response = await server.SendAsync(request))
                {
                    // Assert
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);
                    string content = await response.Content.ReadAsStringAsync();
                    Assert.Equal(json, content);
                }
            }
        }
    }
}
