using Arcus.WebApi.Security.Authentication.Certificates;
using Xunit;

namespace Arcus.WebApi.Tests.Unit.Security.Authentication
{
    public class CertificateAuthenticationAttributeTests
    {
        [Fact]
        public void Attribute_WithWronglyManipulatedArguments_HandlesFault()
        {
            // Arrange
            var attribute = new CertificateAuthenticationAttribute
            {
                Arguments = null
            };

            // Act
            attribute.EmitSecurityEvents = true;
            
            // Assert
            Assert.True(attribute.EmitSecurityEvents);
        }
        
        [Fact]
        public void Attribute_WithEmptyArguments_HandlesFault()
        {
            // Arrange
            var attribute = new CertificateAuthenticationAttribute
            {
                Arguments = new object[0]
            };

            // Act
            attribute.EmitSecurityEvents = true;
            
            // Assert
            Assert.True(attribute.EmitSecurityEvents);
        }

        [Fact]
        public void Attribute_EmitSecurityEvents_Succeeds()
        {
            // Arrange
            var attribute = new CertificateAuthenticationAttribute();

            // Act
            attribute.EmitSecurityEvents = true;
            
            // Assert
            Assert.True(attribute.EmitSecurityEvents);
        }
    }
}
