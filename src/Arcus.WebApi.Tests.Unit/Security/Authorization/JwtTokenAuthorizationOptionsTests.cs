using System;
using System.Collections.Generic;
using Arcus.WebApi.Security.Authorization;
using Arcus.WebApi.Security.Authorization.Jwt;
using Moq;
using Xunit;

namespace Arcus.WebApi.Tests.Unit.Security.Authorization
{
    public class JwtTokenAuthorizationOptionsTests
    {
        [Fact]
        public void DefaultOptions_InitializesProperties_Succeeds()
        {
            // Act
            var options = new JwtTokenAuthorizationOptions();

            // Assert
            Assert.NotNull(options.JwtTokenReader);
            Assert.False(String.IsNullOrWhiteSpace(options.HeaderName));
        }

        [Fact]
        public void CreateOptions_WithoutReader_Fails()
        {
            Assert.ThrowsAny<ArgumentException>(() => new JwtTokenAuthorizationOptions(reader: null));
        }

        [Fact]
        public void CreateOptions_WithHeaderAndWithoutReader_Fails()
        {
            Assert.ThrowsAny<ArgumentException>(
                () => new JwtTokenAuthorizationOptions(reader: null, headerName: "some header name"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        public void CreateOptions_WithBlankHeaderName_Fails(string headerName)
        {
            Assert.ThrowsAny<ArgumentException>(
                () => new JwtTokenAuthorizationOptions(Mock.Of<IJwtTokenReader>(), headerName));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        public void SetBlankHeaderName_InOptions_Fails(string headerName)
        {
            // Arrange
            var options = new JwtTokenAuthorizationOptions();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => options.HeaderName = headerName);
        }

        [Fact]
        public void SetNullReader_InOptions_Fails()
        {
            // Arrange
            var options = new JwtTokenAuthorizationOptions();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() => options.JwtTokenReader = null);
        }

        [Fact]
        public void CreateOptions_WithEmptyClaims_Fails()
        {
            Assert.ThrowsAny<ArgumentException>(
                () => new JwtTokenAuthorizationOptions(new Dictionary<string, string>()));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void CreateOptions_WithBlankKeyInClaims_Fails(string key)
        {
            Assert.ThrowsAny<ArgumentException>(
                () => new JwtTokenAuthorizationOptions(new Dictionary<string, string> { [key] = "some value" }));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void CreateOptions_WithBlankValueInClaims_Fails(string value)
        {
            Assert.ThrowsAny<ArgumentException>(
                () => new JwtTokenAuthorizationOptions(new Dictionary<string, string> { ["some key"] = value }));
        }

        [Fact]
        public void CreateOptions_WithFilledClaims_Succeeds()
        {
            // Arrange
            var claimCheck = new Dictionary<string, string> { ["some key"] = "some value" };

            // Act
            var options = new JwtTokenAuthorizationOptions(claimCheck);

            // Assert
            Assert.NotNull(options);
        }
    }
}
