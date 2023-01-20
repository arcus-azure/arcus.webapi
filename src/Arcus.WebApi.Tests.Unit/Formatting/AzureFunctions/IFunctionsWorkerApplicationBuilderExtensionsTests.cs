using System;
using Arcus.WebApi.Hosting.AzureFunctions.Formatting;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Xunit;

namespace Arcus.WebApi.Tests.Unit.Formatting.AzureFunctions
{
    // ReSharper disable once InconsistentNaming
    public class IFunctionsWorkerApplicationBuilderExtensionsTests
    {
        [Fact]
        public void UseOnlyJsonFormatting_WithDefault_Succeeds()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new Mock<IFunctionsWorkerApplicationBuilder>();
            builder.Setup(b => b.Services).Returns(services);

            // Act
            builder.Object.UseOnlyJsonFormatting();

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            Assert.NotNull(provider.GetService<AzureFunctionsJsonFormattingMiddleware>());
        }
    }
}
