using System.Threading.Tasks;

namespace Arcus.WebApi.Security.Authorization.Jwt
{
    /// <summary>
    /// Contract to verify the JWT token from the HTTP request header.
    /// </summary>
    public interface IJwtTokenReader
    {
        /// <summary>
        ///     Verify if the token is considered valid.
        /// </summary>
        /// <param name="token">The JWT token.</param>
        Task<bool> IsValidTokenAsync(string token);
    }
}