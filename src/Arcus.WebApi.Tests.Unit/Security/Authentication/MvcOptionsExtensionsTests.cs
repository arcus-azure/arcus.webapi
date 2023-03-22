using System;
using Arcus.WebApi.Security.Authentication.Certificates;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Arcus.WebApi.Tests.Unit.Security.Authentication
{
    public class MvcOptionsExtensionsTests
    {
        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddSharedAccessKeyAuthenticationFilterOnHeader_WithoutHeaderName_Fails(string headerName)
        {
            // Arrange
            var options = new MvcOptions();
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => options.AddSharedAccessKeyAuthenticationFilterOnHeader(headerName, "MySecret"));
        }
        
        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddSharedAccessKeyAuthenticationFilterOnHeaderWithOptions_WithoutHeaderName_Fails(string headerName)
        {
            // Arrange
            var options = new MvcOptions();
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => options.AddSharedAccessKeyAuthenticationFilterOnHeader(headerName, "MySecret", options => options.EmitSecurityEvents = true));
        }
        
        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddSharedAccessKeyAuthenticationFilterOnHeader_WithoutSecretName_Fails(string secretName)
        {
            // Arrange
            var options = new MvcOptions();
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => options.AddSharedAccessKeyAuthenticationFilterOnHeader("x-api-key", secretName));
        }
        
        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddSharedAccessKeyAuthenticationFilterOnHeaderWithOptions_WithoutSecretName_Fails(string secretName)
        {
            // Arrange
            var options = new MvcOptions();
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => options.AddSharedAccessKeyAuthenticationFilterOnHeader("x-api-key", secretName, options => options.EmitSecurityEvents = true));
        }
        
        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddSharedAccessKeyAuthenticationFilterOnQuery_WithoutParameterName_Fails(string parameterName)
        {
            // Arrange
            var options = new MvcOptions();
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => options.AddSharedAccessKeyAuthenticationFilterOnHeader(parameterName, "MySecret"));
        }
        
        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddSharedAccessKeyAuthenticationFilterOnQueryWithOptions_WithoutParameterName_Fails(string parameterName)
        {
            // Arrange
            var options = new MvcOptions();
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => options.AddSharedAccessKeyAuthenticationFilterOnQuery(parameterName, "MySecret", options => options.EmitSecurityEvents = true));
        }
        
        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddSharedAccessKeyAuthenticationFilterOnQuery_WithoutSecretName_Fails(string secretName)
        {
            // Arrange
            var options = new MvcOptions();
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => options.AddSharedAccessKeyAuthenticationFilterOnQuery("x-api-key", secretName));
        }
        
        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddSharedAccessKeyAuthenticationFilterOnQueryWithOptions_WithoutSecretName_Fails(string secretName)
        {
            // Arrange
            var options = new MvcOptions();
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => options.AddSharedAccessKeyAuthenticationFilterOnQuery("x-api-key", secretName, options => options.EmitSecurityEvents = true));
        }

        [Fact]
        public void AddCertificateAuthenticationFilter_WithoutConfiguredAuthentication_Fails()
        {
            // Arrange
            var options = new MvcOptions();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => options.AddCertificateAuthenticationFilter(configureAuthentication: null));
        }

        [Fact]
        public void AddCertificateAuthenticationFilter_WithoutConfiguredAuthenticationWithOptions_Fails()
        {
            // Arrange
            var options = new MvcOptions();

            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(
                () => options.AddCertificateAuthenticationFilter(configureAuthentication: null, configureOptions: opt => { }));
        }

        [Fact]
        public void AddCertificateAuthenticationFilter_WithoutConfiguredAuthenticationEntries_Fails()
        {
            // Arrange
            var options = new MvcOptions();

            // Act / Assert
            Assert.ThrowsAny<InvalidOperationException>(
                () => options.AddCertificateAuthenticationFilter(configureAuthentication: auth => { }));
        }

        [Fact]
        public void AddCertificateAuthenticationFilter_WithConfiguredAuthenticationEntry_Succeeds()
        {
            // Arrange
            var options = new MvcOptions();

            // Act / Assert
            options.AddCertificateAuthenticationFilter(auth => auth.WithSubject(X509ValidationLocation.Configuration, "SubjectConfig"));
        }
    }
}
