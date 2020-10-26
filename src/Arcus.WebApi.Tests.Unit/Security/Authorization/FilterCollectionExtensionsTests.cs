using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Filters;
using Xunit;

namespace Arcus.WebApi.Tests.Unit.Security.Authorization
{
    public class FilterCollectionExtensionsTests
    {
        [Fact]
        public void AddJwtTokenAuthorization_WithOptionsWithoutClaimCheck_Fails()
        {
            // Arrange
            var filters = new FilterCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => filters.AddJwtTokenAuthorization(claimCheck: null, configureOptions: options => { }));
        }

        [Fact]
        public void AddJwtTokenAuthorization_WithOptionsWithEmptyClaimCheck_Fails()
        {
            // Arrange
            var filters = new FilterCollection();
            var claimCheck = new Dictionary<string, string>();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => filters.AddJwtTokenAuthorization(claimCheck: claimCheck, configureOptions: options => { }));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddJwtTokenAuthorization_WithOptionsWithBlankKeyInClaimCheck_Fails(string key)
        {
            // Arrange
            var filters = new FilterCollection();
            var claimCheck = new Dictionary<string, string> { [key ?? ""] = "some value" };

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => filters.AddJwtTokenAuthorization(claimCheck: claimCheck, configureOptions: options => { }));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddJwtTokenAuthorization_WithOptionsWithBlankValueInClaimCheck_Fails(string value)
        {
            // Arrange
            var filters = new FilterCollection();
            var claimCheck = new Dictionary<string, string> { ["some key"] = value };

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => filters.AddJwtTokenAuthorization(claimCheck: claimCheck, configureOptions: options => { }));
        }

        [Fact]
        public void AddJwtTokenAuthorization_WithoutClaimCheck_Fails()
        {
            // Arrange
            var filters = new FilterCollection();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => filters.AddJwtTokenAuthorization(claimCheck: null));
        }

        [Fact]
        public void AddJwtTokenAuthorization_WithEmptyClaimCheck_Fails()
        {
            // Arrange
            var filters = new FilterCollection();
            var claimCheck = new Dictionary<string, string>();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => filters.AddJwtTokenAuthorization(claimCheck));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddJwtTokenAuthorization_WithBlankKeyInClaimCheck_Fails(string key)
        {
            // Arrange
            var filters = new FilterCollection();
            var claimCheck = new Dictionary<string, string> { [key ?? ""] = "some value" };

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => filters.AddJwtTokenAuthorization(claimCheck));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddJwtTokenAuthorization_WithBlankValueInClaimCheck_Fails(string value)
        {
            // Arrange
            var filters = new FilterCollection();
            var claimCheck = new Dictionary<string, string> { ["some key"] = value };

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => filters.AddJwtTokenAuthorization(claimCheck));
        }
    }
}
