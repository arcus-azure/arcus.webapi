using System.Threading.Tasks;

namespace Arcus.WebApi.Security.Authorization.Jwt
{
    public interface IJwtTokenReader
    {
        /// <summary>
        ///     Validates if the token is considered valid
        /// </summary>
        /// <param name="token">JWT token</param>
        Task<bool> IsValidToken(string token);
    }
}