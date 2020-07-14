using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arcus.WebApi.Security.Authentication.SharedAccessKey;
using GuardNet;
#if NETCOREAPP3_1
using Microsoft.OpenApi.Models;    
#endif
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Arcus.WebApi.OpenApi.Extensions
{
    /// <summary>
    /// A Swashbuckle operation filter that adds shared access key security definitions to authorized API operations.
    /// </summary>
    public class SharedAccessKeyAuthenticationOperationFilter : IOperationFilter
    {
        private readonly IEnumerable<string> _scopes;

        /// <summary>
        /// Initializes a new instance of the <see cref="SharedAccessKeyAuthenticationOperationFilter"/> class.
        /// </summary>
        /// <param name="scopes">A list of API scopes that is defined for the API that must be documented.</param>
        /// <remarks>It is not possible right now to document the scopes on a fine grained operation-level.</remarks>
        /// <exception cref="ArgumentNullException">When the <paramref name="scopes"/> are <c>null</c>.</exception>
        /// <exception cref="ArgumentException">When the <paramref name="scopes"/> has any elements that are <c>null</c> or blank.</exception>
        public SharedAccessKeyAuthenticationOperationFilter(IEnumerable<string> scopes)
        {
            Guard.NotNull(scopes, nameof(scopes), "The sequence of scopes cannot be null");
            Guard.For<ArgumentException>(() => scopes.Any(String.IsNullOrWhiteSpace), "The sequence of scopes cannot contain a scope that is null or blank");

            _scopes = scopes;
        }

        /// <summary>
        /// Applies the OperationFilter to the API <paramref name="operation"/>.
        /// </summary>
        /// <param name="operation">The operation instance on which the OperationFilter must be applied.</param>
        /// <param name="context">Provides meta-information on the <paramref name="operation"/> instance.</param>
#if NETCOREAPP3_1
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
#else
        public void Apply(Operation operation, OperationFilterContext context)
#endif
        {
            bool hasOperationAuthentication =
                context.MethodInfo
                       .GetCustomAttributes(true)
                       .OfType<SharedAccessKeyAuthenticationAttribute>()
                       .Any();

            bool hasControllerAuthentication =
                context.MethodInfo.DeclaringType != null
                && context.MethodInfo.DeclaringType
                          .GetCustomAttributes(true)
                          .OfType<SharedAccessKeyAuthenticationAttribute>()
                          .Any();

            if (hasOperationAuthentication || hasControllerAuthentication)
            {
                if (operation.Responses.ContainsKey("401") == false)
                {
#if NETCOREAPP3_1
                    operation.Responses.Add("401", new OpenApiResponse { Description = "Unauthorized" });
#else
                    operation.Responses.Add("401", new Response { Description = "Unauthorized" });
#endif
                }

                if (operation.Responses.ContainsKey("403") == false)
                {
#if NETCOREAPP3_1
                    operation.Responses.Add("403", new OpenApiResponse { Description = "Forbidden" });
#else
                    operation.Responses.Add("403", new Response { Description = "Forbidden" });
#endif
                }
#if NETCOREAPP3_1
                var scheme = new OpenApiSecurityScheme
                {
                    Scheme = "sharedaccesskey",
                    Type = SecuritySchemeType.ApiKey
                };

                operation.Security = new List<OpenApiSecurityRequirement>
                {
                    new OpenApiSecurityRequirement
                    {
                        [scheme] = _scopes.ToList()
                    }
                };
#else
                operation.Security = new List<IDictionary<string, IEnumerable<string>>>
                {
                    new Dictionary<string, IEnumerable<string>> { ["sharedaccesskey"] = _scopes }
                };
#endif
            }
        }
    }
}
