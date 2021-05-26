using System;
using Arcus.WebApi.Security.Authentication.SharedAccessKey;
using Xunit;

namespace Arcus.WebApi.Tests.Unit.Security.Authentication
{
    public class SharedAccessKeyAuthenticationAttributeTests
    {
        [Theory]
        [InlineData(null, null, "not empty or whitespace")]
        [InlineData("", "", "not empty or whitespace")]
        [InlineData(" ", " ", "not empty or whitespace")]
        [InlineData("not empty or whitespace", "not empty or whitespace", null)]
        [InlineData("not empty or whitespace", "not empty or whitespace", "")]
        [InlineData("not empty or whitespace", "not empty or whitespace", " ")]
        public void SharedAccessKeyAttribute_WithNotPresentHeaderNameQueryParameterNameAndOrSecretName_ShouldFailWithArgumentException(
            string headerName,
            string queryParameterName,
            string secretName)
        {
            Assert.Throws<ArgumentException>(
                () => new SharedAccessKeyAuthenticationAttribute(headerName: headerName, queryParameterName: queryParameterName, secretName: secretName));
        }

        [Fact]
        public void Attribute_WithWronglyManipulatedArguments_HandlesFault()
        {
            // Arrange
            var attribute = new SharedAccessKeyAuthenticationAttribute("secret-name", "header-value")
            {
                Arguments = null
            };

            // Act / Assert
            attribute.EmitSecurityEvents = true;
        }
        
        [Fact]
        public void Attribute_WithEmptyArguments_HandlesFault()
        {
            // Arrange
            var attribute = new SharedAccessKeyAuthenticationAttribute("secret-name", "header-value")
            {
                Arguments = new object[0]
            };

            // Act
            attribute.EmitSecurityEvents = true;
            
            // Assert
            Assert.True(attribute.EmitSecurityEvents);
        }

        [Fact]
        public void AttributeWithHeader_EmitSecurityEvents_Succeeds()
        {
            // Arrange
            var attribute = new SharedAccessKeyAuthenticationAttribute("secret-name", "header-value");

            // Act
            attribute.EmitSecurityEvents = true;
            
            // Assert
            Assert.True(attribute.EmitSecurityEvents);
        }
        
        [Fact]
        public void AttributeWithQueryParameter_EmitSecurityEvents_Succeeds()
        {
            // Arrange
            var attribute = new SharedAccessKeyAuthenticationAttribute("secret-name", queryParameterName: "parameter-value");

            // Act
            attribute.EmitSecurityEvents = true;
            
            // Assert
            Assert.True(attribute.EmitSecurityEvents);
        }

        [Fact]
        public void AttributeWithHeaderAndQueryParameter_EmitSecurityEvents_Succeeds()
        {
            // Arrange
            var attribute = new SharedAccessKeyAuthenticationAttribute("secret-name", "header-value", "parameter-value");

            // Act
            attribute.EmitSecurityEvents = true;
            
            // Assert
            Assert.True(attribute.EmitSecurityEvents);
        }
    }
}
