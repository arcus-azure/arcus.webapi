﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Arcus.Security.Secrets.Core.Exceptions;
using Arcus.Security.Secrets.Core.Interfaces;
using GuardNet;
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
    [Obsolete("Feature is moved to our 'Arcus.WebApi.Security.Authentication' NuGet package, please use Arcus.WebApi.Security.Authentication.SharedAccessKey.SharedAccessKeyAuthenticationFilter")]
    public class SharedAccessKeyAuthenticationFilter : IAsyncAuthorizationFilter
    {
        private readonly string _headerName, _secretName;

        /// <summary>
        /// Initializes a new instance of the <see cref="SharedAccessKeyAuthenticationFilter"/> class.
        /// </summary>
        /// <param name="headerName">The name of the request header which value must match the stored secret.</param>
        /// <param name="secretName">The name of the secret that's being retrieved using the <see cref="ISecretProvider.Get"/> call.</param>
        /// <exception cref="ArgumentException">When the <paramref name="headerName"/> is <c>null</c> or blank.</exception>
        /// <exception cref="ArgumentException">When the <paramref name="secretName"/> is <c>null</c> or blank.</exception>
        public SharedAccessKeyAuthenticationFilter(string headerName, string secretName)
        {
            Guard.NotNullOrWhitespace(headerName, nameof(headerName), "Header name cannot be blank");
            Guard.NotNullOrWhitespace(secretName, nameof(secretName), "Secret name cannot be blank");

            _headerName = headerName;
            _secretName = secretName;
        }

        /// <summary>
        /// Called early in the filter pipeline to confirm request is authorized.
        /// </summary>
        /// <param name="context">The <see cref="T:Microsoft.AspNetCore.Mvc.Filters.AuthorizationFilterContext" />.</param>
        /// <returns>
        /// A <see cref="T:System.Threading.Tasks.Task" /> that on completion indicates the filter has executed.
        /// </returns>
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            Guard.NotNull(context, nameof(context));
            Guard.NotNull(context.HttpContext, nameof(context.HttpContext));
            Guard.For<ArgumentException>(() => context.HttpContext.Request == null, "Invalid action context given without any HTTP request");
            Guard.For<ArgumentException>(() => context.HttpContext.Request.Headers == null, "Invalid action context given without any HTTP request headers");
            Guard.For<ArgumentException>(() => context.HttpContext.RequestServices == null, "Invalid action context given without any HTTP request services");

            if (context.HttpContext.Request.Headers
                       .TryGetValue(_headerName, out StringValues requestSecretHeaders))
            {
                ISecretProvider userDefinedSecretProvider = 
                    context.HttpContext.RequestServices.GetService<ICachedSecretProvider>()
                    ?? context.HttpContext.RequestServices.GetService<ISecretProvider>();
                
                if (userDefinedSecretProvider == null)
                {
                    throw new KeyNotFoundException(
                        $"No configured {nameof(ICachedSecretProvider)} or {nameof(ISecretProvider)} implementation found in the request service container. "
                        + "Please configure such an implementation (ex. in the Startup) of your application");
                }

                string foundSecret = await userDefinedSecretProvider.Get(_secretName);
                if (foundSecret == null)
                {
                    throw new SecretNotFoundException(_secretName);
                }

                if (requestSecretHeaders.Any(headerValue => !String.Equals(headerValue, foundSecret)))
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