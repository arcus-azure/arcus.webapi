using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Arcus.WebApi.Security.Authorization.Jwt;
using Arcus.WebApi.Tests.Unit.Security.Extension;
using IdentityModel;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Arcus.WebApi.Tests.Unit.Security.Authorization
{
    public class JwtTokenReaderTests
    {
        private const string MicrosoftDiscoveryEndpoint =
            "https://login.microsoftonline.com/common/v2.0/.well-known/openid-configuration";

        [Fact]
        public void Constructor_WithoutClaims_ThrowsException()
        {
            // Arrange
            var claimCheck = new Dictionary<string, string>();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new JwtTokenReader(claimCheck));
        }

        [Fact]
        public void Constructor_WithApplicationId_Succeeds()
        {
            // Arrange
            string applicationId = Guid.NewGuid().ToString();

            // Act
            JwtTokenReader jwtTokenReader = new JwtTokenReader(applicationId);

            // Assert
            Assert.NotNull(jwtTokenReader);
        }
        
        [Fact]
        public void Constructor_WithEmptyApplicationId_Succeeds()
        {
            // Arrange
            string applicationId = string.Empty;

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new JwtTokenReader(applicationId));
        }

        [Fact]
        public void Constructor_WithApplicationIdInClaimCheck_Succeeds()
        {
            // Arrange
            Dictionary<string, string> claimCheck = new Dictionary<string, string>
            {
                {JwtClaimTypes.Audience, Guid.NewGuid().ToString()}
            };

            // Act
            JwtTokenReader jwtTokenReader = new JwtTokenReader(claimCheck);

            // Assert
            Assert.NotNull(jwtTokenReader);
        }

        [Fact]
        public void Constructor_WithTokenValidationParametersAndClaimCheck_Succeeds()
        {
            // Arrange
            TokenValidationParameters tokenValidationParameters = new TokenValidationParameters();

            Dictionary<string, string> claimCheck = new Dictionary<string, string>
            {
                {JwtClaimTypes.Audience, Guid.NewGuid().ToString()}
            };

            // Act
            JwtTokenReader jwtTokenReader = new JwtTokenReader(tokenValidationParameters, claimCheck);

            // Assert
            Assert.NotNull(jwtTokenReader);
        }

        [Fact]
        public async Task IsValidToken_WithTokenValidationParametersAndClaimCheck_InvalidToken_WithException()
        {
            // Arrange
            TokenValidationParameters tokenValidationParameters = new TokenValidationParameters();

            string issuer = Util.GetRandomString(10);
            string authority = Util.GetRandomString(10);
            int daysValid = 7;

            RSA rsa = new RSACryptoServiceProvider(512);
            string privateKey = rsa.ToCustomXmlString(true);

            Dictionary<string, string> claimCheck = new Dictionary<string, string>
            {
                {JwtClaimTypes.Audience, authority}
            };

            string token = CreateJwt(issuer, authority, privateKey, daysValid, claimCheck);

            // Act
            JwtTokenReader jwtTokenReader = new JwtTokenReader(tokenValidationParameters, claimCheck);
            bool isTokenValid = await jwtTokenReader.IsValidTokenAsync(token);

            // Assert
            Assert.False(isTokenValid);
        }

        [Fact]
        public async Task IsValidToken_WithTokenValidationParametersAndClaimCheck_ValidToken()
        {
            // Arrange
            string authority = $"http://{Util.GetRandomString(10).ToLower()}.com";
            string issuer = $"http://{Util.GetRandomString(10).ToLower()}.com";
            
            IdentityModelEventSource.ShowPII = true;
            int daysValid = 7;

            RSA rsa = new RSACryptoServiceProvider(512);
            string privateKey = rsa.ToCustomXmlString(true);

            Dictionary<string, string> claimCheck = new Dictionary<string, string>
            {
                {JwtClaimTypes.Audience, authority}
            };

            TokenValidationParameters tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidIssuer = issuer,
                ValidAudience = authority,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(privateKey))
            };

            string token = CreateJwt(issuer, authority, privateKey, daysValid, claimCheck);

            // Act
            JwtTokenReader jwtTokenReader = new JwtTokenReader(tokenValidationParameters, claimCheck);
            bool isTokenValid = await jwtTokenReader.IsValidTokenAsync(token);

            // Assert
            Assert.True(isTokenValid);
        }

        [Fact]
        public async Task IsValidToken_WithTokenValidationParametersAndClaimCheck_InvalidClaims()
        {
            // Arrange
            string authority = $"http://{Util.GetRandomString(10).ToLower()}.com";
            string issuer = $"http://{Util.GetRandomString(10).ToLower()}.com";
            string oid = Util.GetRandomString(10);

            IdentityModelEventSource.ShowPII = true;
            int daysValid = 7;

            RSA rsa = new RSACryptoServiceProvider(512);
            string privateKey = rsa.ToCustomXmlString(true);

            Dictionary<string, string> claimCheck = new Dictionary<string, string>
            {
                {JwtClaimTypes.Audience, authority},
                {"oid", oid},
                {"uud", Guid.NewGuid().ToString()}
            };

            TokenValidationParameters tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidIssuer = issuer,
                ValidAudience = authority,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(privateKey))
            };

            string token = CreateJwt(issuer, authority, privateKey, daysValid, claimCheck);

            // Act
            JwtTokenReader jwtTokenReader = new JwtTokenReader(tokenValidationParameters, claimCheck);
            bool isTokenValid = await jwtTokenReader.IsValidTokenAsync(token);

            // Assert
            Assert.False(isTokenValid);
        }

        [Fact]
        public async Task IsValidToken_WithTokenValidationParametersAndClaimCheck_EmptyClaims_ThrowsException()
        {
            // Arrange
            string authority = $"http://{Util.GetRandomString(10).ToLower()}.com";
            string issuer = $"http://{Util.GetRandomString(10).ToLower()}.com";

            IdentityModelEventSource.ShowPII = true;

            RSA rsa = new RSACryptoServiceProvider(512);
            string privateKey = rsa.ToCustomXmlString(true);

            Dictionary<string, string> claimCheck = new Dictionary<string, string>();

            TokenValidationParameters tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidIssuer = issuer,
                ValidAudience = authority,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(privateKey))
            };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new JwtTokenReader(tokenValidationParameters, claimCheck));
        }

        [Fact]
        public async Task IsValidToken_WithTokenValidationParameters_NullClaims_ValidToken()
        {
            // Arrange
            string authority = $"http://{Util.GetRandomString(10).ToLower()}.com";
            string issuer = $"http://{Util.GetRandomString(10).ToLower()}.com";

            IdentityModelEventSource.ShowPII = true;
            int daysValid = 7;

            RSA rsa = new RSACryptoServiceProvider(512);
            string privateKey = rsa.ToCustomXmlString(true);

            TokenValidationParameters tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidIssuer = issuer,
                ValidAudience = authority,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(privateKey))
            };

            string token = CreateJwt(issuer, authority, privateKey, daysValid);

            // Act
            JwtTokenReader jwtTokenReader = new JwtTokenReader(tokenValidationParameters);
            bool isTokenValid = await jwtTokenReader.IsValidTokenAsync(token);

            // Assert
            Assert.True(isTokenValid);
        }

        [Fact]
        public void Constructor_WithTokenValidationParametersButWithoutClaimCheck_Succeeds()
        {
            // Arrange
            TokenValidationParameters tokenValidationParameters = new TokenValidationParameters();

            // Act
            JwtTokenReader jwtTokenReader = new JwtTokenReader(tokenValidationParameters);

            // Assert
            Assert.NotNull(jwtTokenReader);
        }

        [Fact]
        public void Constructor_WithTokenValidationParametersAndClaimCheckButWithoutDiscoveryEndpoint_ThrowsException()
        {
            // Arrange
            TokenValidationParameters tokenValidationParameters = new TokenValidationParameters();
            string openIdConnectDiscoveryUri = string.Empty;

            var claimCheck = new Dictionary<string, string>
            {
                {JwtClaimTypes.Audience, Guid.NewGuid().ToString()}
            };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new JwtTokenReader(tokenValidationParameters, openIdConnectDiscoveryUri, claimCheck));
        }

        [Fact]
        public void Constructor_WithTokenValidationParametersButWithoutDiscoveryEndpointAndClaimCheck_ThrowsException()
        {
            // Arrange
            TokenValidationParameters tokenValidationParameters = new TokenValidationParameters();
            string openIdConnectDiscoveryUri = string.Empty;

            var claimCheck = new Dictionary<string, string>();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new JwtTokenReader(tokenValidationParameters, openIdConnectDiscoveryUri, claimCheck));
        }

        [Fact]
        public void Constructor_WithoutTokenValidationParametersAndClaimCheck_Succeeds()
        {
            // Arrange
            TokenValidationParameters tokenValidationParameters = null;

            var claimCheck = new Dictionary<string, string>();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new JwtTokenReader(tokenValidationParameters, claimCheck));
        }

        [Fact]
        public void Constructor_WithTokenValidationParametersAndDiscoveryEndpoint_Succeeds()
        {
            // Arrange
            TokenValidationParameters tokenValidationParameters = new TokenValidationParameters();
            string openIdConnectDiscoveryUri = "https://login.microsoftonline.com/common/v2.0/.well-known/openid-configuration";

            // Act
            var jwtTokenReader = new JwtTokenReader(tokenValidationParameters, openIdConnectDiscoveryUri);

            // Assert
            Assert.NotNull(jwtTokenReader);
        }

        [Fact]
        public void Constructor_WithClaimCheck_Succeeds()
        {
            // Arrange
            string authority = $"http://{Util.GetRandomString(10).ToLower()}.com";
            string oid = Util.GetRandomString(10);
            string aad = Util.GetRandomString(10);

            TokenValidationParameters tokenValidationParameters = new TokenValidationParameters();
            string openIdConnectDiscoveryUri = "https://login.microsoftonline.com/common/v2.0/.well-known/openid-configuration";

            Dictionary<string, string> claimCheck = new Dictionary<string, string>
            {
                {JwtClaimTypes.Audience, authority},
                {"oid", oid},
                {"aad", aad}
            };

            // Act
            var jwtTokenReader = new JwtTokenReader(tokenValidationParameters, openIdConnectDiscoveryUri, claimCheck);

            // Assert
            Assert.NotNull(jwtTokenReader);
        }

        [Fact]
        public async Task Constructor_ValidateClaimCheck_Succeeds()
        {
            // Arrange
            string issuer = $"http://{Util.GetRandomString(10).ToLower()}.com";
            string oid = Util.GetRandomString(10);
            string aad = Util.GetRandomString(10);
            string authority = $"http://{Util.GetRandomString(10).ToLower()}.com";
            int daysValid = 7;

            RSA rsa = new RSACryptoServiceProvider(512);
            string privateKey = rsa.ToCustomXmlString(true);

            Dictionary<string, string> claimCheck = new Dictionary<string, string>
            {
                {JwtClaimTypes.Audience, authority},
                {"oid", oid},
                {"aad", aad}
            };

            string token = CreateJwt(issuer, authority, privateKey, daysValid, claimCheck);
            JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
            JwtSecurityToken securityToken = handler.ReadJwtToken(token);

            // Act
            JwtTokenReader jwtTokenReader = new JwtTokenReader(claimCheck);
            bool isValid = jwtTokenReader.ValidateClaimCheck(securityToken);

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public async Task Constructor_ValidateClaimCheck_Not_In_Jwt_Claims()
        {
            // Arrange
            string issuer = $"http://{Util.GetRandomString(10).ToLower()}.com";
            string oid = Util.GetRandomString(10);
            string aad = Util.GetRandomString(10);
            string authority = $"http://{Util.GetRandomString(10).ToLower()}.com";
            int daysValid = 7;

            RSA rsa = new RSACryptoServiceProvider(512);
            string privateKey = rsa.ToCustomXmlString(true);

            Dictionary<string, string> claimCheck = new Dictionary<string, string>
            {
                {JwtClaimTypes.Audience, authority},
                {"oid", oid},
                {"uud", Guid.NewGuid().ToString()},
                {"aad", aad}
            };

            string token = CreateJwt(issuer, authority, privateKey, daysValid, claimCheck);
            JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
            JwtSecurityToken securityToken = handler.ReadJwtToken(token);

            // Act
            JwtTokenReader jwtTokenReader = new JwtTokenReader(claimCheck);
            bool isValid = jwtTokenReader.ValidateClaimCheck(securityToken);

            // Assert
            Assert.False(isValid);
        }

        private static string CreateJwt(string issuer, string authority, string symSec, int daysValid, IDictionary<string, string> claims = null)
        {
            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
            ClaimsIdentity claimsIdentity = null;

            if (claims != null)
            {
                claimsIdentity = CreateClaimsIdentities(claims);
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = claimsIdentity,
                Expires = DateTime.UtcNow.AddDays(daysValid),
                Audience = authority,
                Issuer = issuer,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.Default.GetBytes(symSec)), SecurityAlgorithms.HmacSha256Signature)
            };

            JwtSecurityToken token = tokenHandler.CreateJwtSecurityToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }

        private static ClaimsIdentity CreateClaimsIdentities(IDictionary<string, string> claims)
        {
            ClaimsIdentity claimsIdentity = new ClaimsIdentity();

            if (claims.ContainsKey("oid"))
                claimsIdentity.AddClaim(new Claim("oid", claims["oid"]));
            
            if (claims.ContainsKey("aad"))
                claimsIdentity.AddClaim(new Claim("aad", claims["aad"]));

            return claimsIdentity;
        }
    }
}