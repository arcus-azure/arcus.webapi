using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Arcus.WebApi.OpenApi.Extensions
{
    /// <summary>
    /// A Swashbuckle operation filter that adds OAuth security definitions to authorized API operations.
    /// </summary>
    public class OAuthAuthorizeOperationFilter : IOperationFilter
    {
        private readonly IEnumerable<string> _scopes;

        /// <summary>
        /// Initializes a new instance of the <see cref="OAuthAuthorizeOperationFilter"/> class.
        /// </summary>
        public OAuthAuthorizeOperationFilter(IEnumerable<string> scopes)
        {
            _scopes = scopes;
        }

        public void Apply(Operation operation, OperationFilterContext context)
        {
            var hasAuthorize = context.MethodInfo.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any() ||
                               (
                                   context.MethodInfo.DeclaringType.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any() &&
                                   context.MethodInfo.GetCustomAttributes(false).OfType<AllowAnonymousAttribute>().Any() == false
                               );

            if (hasAuthorize)
            {
                if (operation.Responses.ContainsKey("401") == false)
                {
                    operation.Responses.Add("401", new Response { Description = "Unauthorized" });
                }

                if (operation.Responses.ContainsKey("403") == false)
                {
                    operation.Responses.Add("403", new Response { Description = "Forbidden" });
                }

                operation.Security = new List<IDictionary<string, IEnumerable<string>>>
                {
                    new Dictionary<string, IEnumerable<string>>
                    {
                        { "oauth2", _scopes }
                    }
                };
            }
        }
    }
}
