using System;

namespace Arcus.WebApi.Security.Authorization
{
    /// <summary>
    /// Attribute to circumvent the JWT token authorization.
    /// </summary>
    public class BypassJwtTokenAuthorizationAttribute : Attribute
    {
    }
}
