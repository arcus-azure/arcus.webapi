using System.Threading.Tasks;
using Arcus.WebApi.Security.Authorization.Jwt;

namespace Arcus.WebApi.Tests.Integration.Security.Authorization.Fixture
{
    /// <summary>
    /// Represents an <see cref="IJwtTokenReader"/> that always returns <c>true</c> for <see cref="IJwtTokenReader.IsValidTokenAsync(string)"/>.
    /// </summary>
    public class IgnoredJwtTokenReader : IJwtTokenReader
    {
        /// <summary>
        ///     Verify if the token is considered valid.
        /// </summary>
        /// <param name="token">The JWT token.</param>
        public Task<bool> IsValidTokenAsync(string token)
        {
            return Task.FromResult(true);
        }
    }
}
