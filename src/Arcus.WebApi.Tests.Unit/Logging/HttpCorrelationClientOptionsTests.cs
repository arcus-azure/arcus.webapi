using System;
using System.Collections.Generic;
using Arcus.WebApi.Logging.Core.Correlation;
using Xunit;

namespace Arcus.WebApi.Tests.Unit.Logging
{
    public class HttpCorrelationClientOptionsTests
    {
        [Fact]
        public void TransactionIdHeaderName_WithNoAction_UsesDefault()
        {
            // Arrange
            var options = new HttpCorrelationClientOptions();

            // Act
            string headerName = options.TransactionIdHeaderName;

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(headerName));
        }

        [Fact]
        public void TransactionIdHeaderName_SetValue_UsesValue()
        {
            // Arrange
            var options = new HttpCorrelationClientOptions();
            string headerName = Guid.NewGuid().ToString();

            // Act
            options.TransactionIdHeaderName = headerName;

            // Assert
            Assert.True(string.IsNullOrWhiteSpace(headerName));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void TransactionIdHeaderName_WithoutValue_Fails(string headerName)
        {
            // Arrange
            var options = new HttpCorrelationClientOptions();

            // ACt / Assert
            Assert.ThrowsAny<ArgumentException>(() => options.TransactionIdHeaderName = headerName);
        }

        [Fact]
        public void UpstreamServiceHeaderName_WithNoAction_UsesDefault()
        {
            // Arrange
            var options = new HttpCorrelationClientOptions();

            // Act
            string headerName = options.UpstreamServiceHeaderName;

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(headerName));
        }

        [Fact]
        public void UpstreamServiceHeaderName_SetValue_UsesValue()
        {
            // Arrange
            var options = new HttpCorrelationClientOptions();
            string headerName = Guid.NewGuid().ToString();

            // Act
            options.UpstreamServiceHeaderName = headerName;

            // Assert
            Assert.True(string.IsNullOrWhiteSpace(headerName));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void UpstreamServiceHeaderName_WithoutValue_Fails(string headerName)
        {
            // Arrange
            var options = new HttpCorrelationClientOptions();

            // ACt / Assert
            Assert.ThrowsAny<ArgumentException>(() => options.UpstreamServiceHeaderName = headerName);
        }

        [Fact]
        public void GenerateDependencyId_WithoutAction_GeneratesDefault()
        {
            // Arrange
            var options = new HttpCorrelationClientOptions();

            // Act
            string dependencyId = options.GenerateDependencyId();

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(dependencyId));
        }

        [Fact]
        public void GenerateDependencyId_WithValue_UsesValue()
        {
            // Arrange
            var options = new HttpCorrelationClientOptions();
            string dependencyId = Guid.NewGuid().ToString();

            // Act
            options.GenerateDependencyId = () => dependencyId;

            // Assert
            Assert.Equal(dependencyId, options.GenerateDependencyId());
        }

        [Fact]
        public void GenerateDependencyId_WithoutValue_Fails()
        {
            // Arrange
            var options = new HttpCorrelationClientOptions();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => options.GenerateDependencyId = null);
        }

        [Fact]
        public void AddTelemetryContext_WithoutValue_Fails()
        {
            // Arrange
            var options = new HttpCorrelationClientOptions();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => options.AddTelemetryContext(telemetryContext: null));
        }
    }
}
