using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Arcus.WebApi.Tests.Unit.Security.Authorization
{
    public class MvcOptionsExtensionsTests
    {
        [Fact]
        public void AddJwtTokenAuthorizationFilter_WithOptionsWithoutClaimCheck_Fails()
        {
            // Arrange
            var options = new MvcOptions();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => options.AddJwtTokenAuthorizationFilter(claimCheck: null, configureOptions: opt => { }));
        }

        [Fact]
        public void AddJwtTokenAuthorizationFilter_WithOptionsWithEmptyClaimCheck_Fails()
        {
            // Arrange
            var options = new MvcOptions();
            var claimCheck = new Dictionary<string, string>();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => options.AddJwtTokenAuthorizationFilter(claimCheck: claimCheck, configureOptions: opt => { }));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddJwtTokenAuthorizationFilter_WithOptionsWithBlankKeyInClaimCheck_Fails(string key)
        {
            // Arrange
            var options = new MvcOptions();
            var claimCheck = new Dictionary<string, string> { [key ?? ""] = "some value" };

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => options.AddJwtTokenAuthorizationFilter(claimCheck: claimCheck, configureOptions: opt => { }));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddJwtTokenAuthorizationFilter_WithOptionsWithBlankValueInClaimCheck_Fails(string value)
        {
            // Arrange
            var options = new MvcOptions();
            var claimCheck = new Dictionary<string, string> { ["some key"] = value };

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => options.AddJwtTokenAuthorizationFilter(claimCheck: claimCheck, configureOptions: opt => { }));
        }

        [Fact]
        public void AddJwtTokenAuthorizationFilter_WithoutClaimCheck_Fails()
        {
            // Arrange
            var options = new MvcOptions();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => options.AddJwtTokenAuthorizationFilter(claimCheck: null));
        }

        [Fact]
        public void AddJwtTokenAuthorizationFilter_WithEmptyClaimCheck_Fails()
        {
            // Arrange
            var options = new MvcOptions();
            var claimCheck = new Dictionary<string, string>();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => options.AddJwtTokenAuthorizationFilter(claimCheck));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddJwtTokenAuthorizationFilter_WithBlankKeyInClaimCheck_Fails(string key)
        {
            // Arrange
            var options = new MvcOptions();
            var claimCheck = new Dictionary<string, string> { [key ?? ""] = "some value" };

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => options.AddJwtTokenAuthorizationFilter(claimCheck));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddJwtTokenAuthorizationFilter_WithBlankValueInClaimCheck_Fails(string value)
        {
            // Arrange
            var options = new MvcOptions();
            var claimCheck = new Dictionary<string, string> { ["some key"] = value };

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => options.AddJwtTokenAuthorizationFilter(claimCheck));
        }
    }
}
