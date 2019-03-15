using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Threading.Tasks;
using Arcus.Security.Secrets.Core.Caching;
using Arcus.Security.Secrets.Core.Exceptions;
using Arcus.Security.Secrets.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;

namespace Arcus.WebApi.Security.Authentication
{
    /// <summary>
    /// Authentication filter to secure HTTP requests with shared access keys.
    /// </summary>
    /// <remarks>
    ///     Please provide an <see cref="ISecretProvider"/> implementation in the configured services of the request.
    /// </remarks>
    public class SharedAccessKeyAuthenticationAttribute : ActionFilterAttribute
    {
        private readonly string _headerName, _secretName;

        /// <summary>
        /// Initializes a new instance of the <see cref="SharedAccessKeyAuthenticationAttribute"/> class.
        /// </summary>
        /// <param name="headerName">The name of the request header which value must match the stored secret.</param>
        /// <param name="secretName">The name of the secret that's being retrieved using the <see cref="ISecretProvider.Get"/> call.</param>
        public SharedAccessKeyAuthenticationAttribute(string headerName, string secretName)
        {
            if (String.IsNullOrWhiteSpace(headerName))
            {
                throw new ArgumentException("Header name cannot be blank", nameof(headerName));
            }

            if (String.IsNullOrWhiteSpace(secretName))
            {
                throw new ArgumentException("Secret name cannot be blank", nameof(secretName));
            }

            _headerName = headerName;
            _secretName = secretName;
        }
        
        /// <inheritdoc />
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.HttpContext?.Request == null)
            {
                throw new ArgumentException(
                    "Invalid action context given without any HTTP request", 
                    nameof(context.HttpContext.Request));
            }

            if (context.HttpContext.Request.Headers == null)
            {
                throw new ArgumentException(
                    "Invalid action context given without any HTTP request headers",
                    nameof(context.HttpContext.Request.Headers));
            }

            if (context.HttpContext.RequestServices == null)
            {
                throw new ArgumentException(
                    "Invalid action context given without any HTTP request services", 
                    nameof(context.HttpContext.RequestServices));
            }

            if (context.HttpContext.Request.Headers
                       .TryGetValue(_headerName, out StringValues requestSecretHeaders))
            {
                var userDefinedSecretProvider = context.HttpContext.RequestServices.GetService<ISecretProvider>();
                if (userDefinedSecretProvider == null)
                {
                    throw new KeyNotFoundException(
                        $"No configured {nameof(ISecretProvider)} implementation found in the request service container. "
                        + "Please configure such an implementation (ex. in the Startup) of your application");
                }

                string foundSecret = await userDefinedSecretProvider.Get(_secretName);
                if (foundSecret == null)
                {
                    throw new SecretNotFoundException(
                        $"No secret found with name {_secretName} in {nameof(ISecretProvider)} implementation {userDefinedSecretProvider.GetType().Name}");
                }

                if (requestSecretHeaders.Any(h => !String.Equals(h, foundSecret)))
                {
                    context.Result = new UnauthorizedResult();
                }
            }
            else
            {
                context.Result = new UnauthorizedResult();
            }
        }
    }
}
