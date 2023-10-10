using System.Collections.Generic;
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

        private static IEnumerable<T> CreateRandomSubset<T>(params T[] formatters)
        {
            int count = BogusGenerator.Random.Int(5, 10);
            return BogusGenerator.Random.Shuffle(
                BogusGenerator.Make(count, () => BogusGenerator.PickRandom(formatters)));
        }
    }
}
