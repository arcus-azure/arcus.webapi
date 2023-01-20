using Bogus;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Arcus.WebApi.Tests.Unit.Logging
{
    // ReSharper disable once InconsistentNaming
    public class IHeaderDictionaryExtensionsTests
    {
        private static readonly Faker BogusGenerator = new Faker();

        [Fact]
        public void GetTraceParent_WithAvailableHeaderValue_Succeeds()
        {
            // Arrange
            string value = BogusGenerator.Random.AlphaNumeric(100);
            var headers = new HeaderDictionary();
            headers["traceparent"] = value;

            // Act
            string actual = headers.GetTraceParent();

            // Assert
            Assert.StartsWith(actual, value);
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void GetTraceParent_WithoutHeaderValue_Succeeds(string value)
        {
            // Arrange
            var headers = new HeaderDictionary();
            headers["traceparent"] = value;

            // Act
            string actual = headers.GetTraceParent();

            // Assert
            Assert.True(string.IsNullOrWhiteSpace(actual));
        }

        [Fact]
        public void GetTraceParent_WithoutHeader_Succeeds()
        {
            // Arrange
            var headers = new HeaderDictionary();

            // Act
            string actual = headers.GetTraceParent();

            // Assert
            Assert.Null(actual);
        }
    }
}
