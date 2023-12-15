using System;
using System.Text.Encodings.Web;
using Arcus.Security.Core;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Arcus.WebApi.Tests.Unit.Security.Authentication
{
    public class AuthenticationBuilderExtensionsTests
    {
        [Fact]
        public void AddJwtBearer_WithServiceProvider_Succeeds()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton(Mock.Of<ISystemClock>());
            services.AddSingleton(UrlEncoder.Default);
            services.AddSecretStore(stores => stores.AddInMemory());
            services.AddSingleton(Mock.Of<TimeProvider>());
            var builder = new AuthenticationBuilder(services);
            
            // Act
            builder.AddJwtBearer((opt, provider) =>
            {
                Assert.NotNull(opt);
                Assert.NotNull(provider);
                Assert.NotNull(provider.GetService<ISecretProvider>());
            });

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            Assert.NotNull(provider.GetService<IConfigureOptions<JwtBearerOptions>>());
            Assert.NotNull(provider.GetService<IPostConfigureOptions<JwtBearerOptions>>());
            Assert.NotNull(provider.GetService<JwtBearerHandler>());
        }

        [Fact]
        public void AddJwtBearer_WithoutOptions_Succeeds()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton(Mock.Of<ISystemClock>());
            services.AddSingleton(UrlEncoder.Default);
            services.AddSingleton(Mock.Of<TimeProvider>());
            var builder = new AuthenticationBuilder(services);
            
            // Act
            builder.AddJwtBearer(configureOptions: (Action<JwtBearerOptions, IServiceProvider>) null);

            // Assert
            IServiceProvider provider = services.BuildServiceProvider();
            Assert.NotNull(provider.GetService<IPostConfigureOptions<JwtBearerOptions>>());
            Assert.NotNull(provider.GetService<JwtBearerHandler>());
        }
    }
}
