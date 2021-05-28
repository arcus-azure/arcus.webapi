using System;
using Microsoft.AspNetCore.Mvc.Filters;
using Xunit;

namespace Arcus.WebApi.Tests.Unit.Security.Authentication
{
    public class FilterCollectionExtensionsTests
    {
        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddSharedAccessKeyAuthenticationOnHeader_WithoutHeaderName_Fails(string headerName)
        {
            // Arrange
            var filters = new FilterCollection();
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() =>
                filters.AddSharedAccessAuthenticationOnHeader(headerName, "MySecret"));
        }
        
        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddSharedAccessKeyAuthenticationOnHeaderWithOptions_WithoutHeaderName_Fails(string headerName)
        {
            // Arrange
            var filters = new FilterCollection();
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() =>
                filters.AddSharedAccessAuthenticationOnHeader(headerName, "MySecret", options => options.EmitSecurityEvents = true));
        }
        
        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddSharedAccessKeyAuthenticationOnHeader_WithoutSecretName_Fails(string secretName)
        {
            // Arrange
            var filters = new FilterCollection();
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() =>
                filters.AddSharedAccessAuthenticationOnHeader("x-api-key", secretName));
        }
        
        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddSharedAccessKeyAuthenticationOnHeaderWithOptions_WithoutSecretName_Fails(string secretName)
        {
            // Arrange
            var filters = new FilterCollection();
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() =>
                filters.AddSharedAccessAuthenticationOnHeader("x-api-key", secretName, options => options.EmitSecurityEvents = true));
        }
        
        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddSharedAccessKeyAuthenticationOnQuery_WithoutParameterName_Fails(string parameterName)
        {
            // Arrange
            var filters = new FilterCollection();
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() =>
                filters.AddSharedAccessAuthenticationOnHeader(parameterName, "MySecret"));
        }
        
        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddSharedAccessKeyAuthenticationOnQueryWithOptions_WithoutParameterName_Fails(string parameterName)
        {
            // Arrange
            var filters = new FilterCollection();
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() =>
                filters.AddSharedAccessAuthenticationOnQuery(parameterName, "MySecret", options => options.EmitSecurityEvents = true));
        }
        
        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddSharedAccessKeyAuthenticationOnQuery_WithoutSecretName_Fails(string secretName)
        {
            // Arrange
            var filters = new FilterCollection();
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() =>
                filters.AddSharedAccessAuthenticationOnQuery("x-api-key", secretName));
        }
        
        [Theory]
        [ClassData(typeof(Blanks))]
        public void AddSharedAccessKeyAuthenticationOnQueryWithOptions_WithoutSecretName_Fails(string secretName)
        {
            // Arrange
            var filters = new FilterCollection();
            
            // Act / Assert
            Assert.ThrowsAny<ArgumentException>(() =>
                filters.AddSharedAccessAuthenticationOnQuery("x-api-key", secretName, options => options.EmitSecurityEvents = true));
        }
    }
}
