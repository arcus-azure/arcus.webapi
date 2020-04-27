using System.Threading.Tasks;

namespace Arcus.WebApi.Security.Authorization.Jwt
{
    public interface IJwtTokenReader
    {
        /// <summary>
        ///     Verify if the token is considered valid
        /// </summary>
        /// <param name="token">JWT token</param>
        Task<bool> IsValidTokenAsync(string token);
    }
}