using System.Collections.Generic;
using System.Text.Json;
using Bogus;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Arcus.WebApi.Tests.Unit.Formatting
{
    public class MvcOptionsExtensionsTests
    {
        private static readonly Faker BogusGenerator = new Faker();

        [Fact]
        public void OnlyAllowJsonFormatting_WithDefault_ConfiguresFormatters()
        {
            // Arrange
            var options = new MvcOptions();
            IEnumerable<IInputFormatter> inputFormatters = CreateRandomSubset(
                Mock.Of<IInputFormatter>(),
                new XmlDataContractSerializerInputFormatter(new MvcOptions()),
                new DummyInputFormatter(),
                new SystemTextJsonInputFormatter(new JsonOptions(), NullLogger<SystemTextJsonInputFormatter>.Instance),
                new XmlDataContractSerializerInputFormatter(new MvcOptions()));
            Assert.All(inputFormatters, formatter => options.InputFormatters.Add(formatter));

            IEnumerable<StringOutputFormatter> outputFormatters = CreateRandomSubset(new StringOutputFormatter());
            Assert.All(outputFormatters, formatter => options.OutputFormatters.Add(formatter));

            // Act
            options.OnlyAllowJsonFormatting();

            // Assert
            Assert.All(options.InputFormatters, formatter => Assert.IsType<SystemTextJsonInputFormatter>(formatter));
            Assert.Empty(options.OutputFormatters);
        }

        [Fact]
        public void ConfigureJsonFormatting_WithOptions_ConfiguresFormatters()
        {
            // Arrange
            var options = new MvcOptions();
            IEnumerable<SystemTextJsonInputFormatter> inputFormatters = 
                CreateRandomSubset(new SystemTextJsonInputFormatter(new JsonOptions(), NullLogger<SystemTextJsonInputFormatter>.Instance));
            Assert.All(inputFormatters, formatter => options.InputFormatters.Add(formatter));
            IEnumerable<SystemTextJsonOutputFormatter> outputFormatters = 
                CreateRandomSubset(new SystemTextJsonOutputFormatter(new JsonSerializerOptions()));
            Assert.All(outputFormatters, formatter => options.OutputFormatters.Add(formatter));
            bool allowTrailingCommas = BogusGenerator.Random.Bool();

            // Act
            options.ConfigureJsonFormatting(opt => opt.AllowTrailingCommas = allowTrailingCommas);

            // Assert
            Assert.All(options.InputFormatters, formatter =>
            {
                var jsonFormatter = Assert.IsType<SystemTextJsonInputFormatter>(formatter);
                Assert.Equal(allowTrailingCommas, jsonFormatter.SerializerOptions.AllowTrailingCommas);
            });
            Assert.All(options.OutputFormatters, formatter =>
            {
                var jsonFormatter = Assert.IsType<SystemTextJsonOutputFormatter>(formatter);
                Assert.Equal(allowTrailingCommas, jsonFormatter.SerializerOptions.AllowTrailingCommas);
            });
        }

        private static IEnumerable<T> CreateRandomSubset<T>(params T[] formatters)
        {
            int count = BogusGenerator.Random.Int(5, 10);
            return BogusGenerator.Random.Shuffle(
                BogusGenerator.Make(count, () => BogusGenerator.PickRandom(formatters)));
        }
    }
}
