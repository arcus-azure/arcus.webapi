using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Arcus.WebApi.Tests.Unit.Security.Authentication
{
    // ReSharper disable once InconsistentNaming
    public class IServiceCollectionExtensionsTests
    {
        [Fact]
        public void AddCertificateAuthenticationValidator_WithoutCertificateConfigurationLocations_Fails()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => services.AddCertificateAuthenticationValidation(configureAuthentication: null));
        }
    }
}
