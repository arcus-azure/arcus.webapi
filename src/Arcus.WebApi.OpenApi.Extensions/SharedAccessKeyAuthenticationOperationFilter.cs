using System.Collections.Generic;
using System.Linq;
using Arcus.WebApi.Security.Authentication.SharedAccessKey;
#if !NETSTANDARD2_1
using System;
using Microsoft.OpenApi.Models;
#else
using System;
using Swashbuckle.AspNetCore.Swagger;
#endif
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Arcus.WebApi.OpenApi.Extensions
{
    /// <summary>
    /// A Swashbuckle operation filter that adds shared access key security definitions to authorized API operations.
    /// </summary>
    public class SharedAccessKeyAuthenticationOperationFilter : IOperationFilter
    {
#if !NETSTANDARD2_1
        private const string DefaultSecuritySchemeName = "sharedaccesskey";

        private readonly SecuritySchemeType _securitySchemeType;
#endif
        private readonly string _securitySchemeName;

#if !NETSTANDARD2_1
        /// <summary>
        /// Initializes a new instance of the <see cref="SharedAccessKeyAuthenticationOperationFilter"/> class.
        /// </summary>
        /// <param name="securitySchemeName">The name of the security scheme. Default value is <c>"sharedaccesskey"</c>.</param>
        /// <param name="securitySchemeType">The type of the security scheme. Default value is <c>ApiKey</c>.</param>
        public SharedAccessKeyAuthenticationOperationFilter(
            string securitySchemeName = DefaultSecuritySchemeName,
            SecuritySchemeType securitySchemeType = SecuritySchemeType.ApiKey)
        {
            if (string.IsNullOrWhiteSpace(securitySchemeName))
            {
                throw new ArgumentException("Requires a name for the Shared Access Key security scheme", nameof(securitySchemeName));
            }

            if (!Enum.IsDefined(typeof(SecuritySchemeType), securitySchemeType))
            {
                throw new ArgumentException("Requires a security scheme type for the Shared Access Key authentication that is within the bounds of the enumeration", nameof(securitySchemeType));
            }

            _securitySchemeName = securitySchemeName;
            _securitySchemeType = securitySchemeType;
        }
#else
        /// <summary>
        /// Initializes a new instance of the <see cref="SharedAccessKeyAuthenticationOperationFilter"/> class.
        /// </summary>
        /// <param name="securitySchemeName">The name of the security scheme. Default value is <c>"sharedaccesskey"</c>.</param>
        public SharedAccessKeyAuthenticationOperationFilter(string securitySchemeName)
        {
            if (string.IsNullOrWhiteSpace(securitySchemeName))
            {
                throw new ArgumentException("Requires a name for the Shared Access Key security scheme", nameof(securitySchemeName));
            }

            _securitySchemeName = securitySchemeName;
        }
#endif

        /// <summary>
        /// Applies the OperationFilter to the API <paramref name="operation"/>.
        /// </summary>
        /// <param name="operation">The operation instance on which the OperationFilter must be applied.</param>
        /// <param name="context">Provides meta-information on the <paramref name="operation"/> instance.</param>
#if !NETSTANDARD2_1
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
#if !NETSTANDARD2_1
                    operation.Responses.Add("401", new OpenApiResponse { Description = "Unauthorized" });
#else
                    operation.Responses.Add("401", new Response { Description = "Unauthorized" });
#endif
                }

                if (operation.Responses.ContainsKey("403") == false)
                {
#if !NETSTANDARD2_1
                    operation.Responses.Add("403", new OpenApiResponse { Description = "Forbidden" });
#else
                    operation.Responses.Add("403", new Response { Description = "Forbidden" });
#endif
                }
#if !NETSTANDARD2_1
                var scheme = new OpenApiSecurityScheme
                {
                    Scheme = _securitySchemeName,
                    Type = _securitySchemeType
                };

                operation.Security = new List<OpenApiSecurityRequirement>
                {
                    new OpenApiSecurityRequirement
                    {
                        [scheme] = new List<string>()
                    }
                };
#else
                operation.Security = new List<IDictionary<string, IEnumerable<string>>>
                {
                    new Dictionary<string, IEnumerable<string>> { [_securitySchemeName] = Enumerable.Empty<string>() }
                };
#endif
            }
        }
    }
}
