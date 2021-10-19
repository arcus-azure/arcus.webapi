using System;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Arcus.WebApi.Tests.Unit.Hosting.Formatting
{
    public class MvcOptionsExtensionsTests
    {
        [Fact]
        public void ConfigureJsonFormatting_WithoutConfigureFunction_Fails()
        {
            // Arrange
            var options = new MvcOptions();
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => options.ConfigureJsonFormatting(configureOptions: null));
        }
    }
}
