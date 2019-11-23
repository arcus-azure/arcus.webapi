using System;
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

namespace Arcus.WebApi.Security.Authentication.SharedAccessKey
{
    /// <summary>
    /// Authentication filter to secure HTTP requests with shared access keys.
    /// </summary>
    /// <remarks>
    ///     Please provide an <see cref="ISecretProvider"/> implementation in the configured services of the request.
    /// </remarks>
    public abstract class SharedAccessKeyAuthenticationFilter : IAsyncAuthorizationFilter
    {
        private readonly string _secretName;

        /// <summary>
        /// Initializes a new instance of the <see cref="SharedAccessKeyAuthenticationFilter"/> class.
        /// </summary>
        /// <param name="secretName">The name of the secret that's being retrieved using the <see cref="ISecretProvider.Get"/> call.</param>
        /// <exception cref="ArgumentException">When the <paramref name="secretName"/> is <c>null</c> or blank.</exception>
        protected SharedAccessKeyAuthenticationFilter(string secretName)
        {
            Guard.NotNullOrWhitespace(secretName, nameof(secretName), "Secret name cannot be blank");

            _secretName = secretName;
        }

        /// <summary>
        /// Called early in the filter pipeline to confirm request is authorized.
        /// </summary>
        /// <param name="context">The <see cref="T:Microsoft.AspNetCore.Mvc.Filters.AuthorizationFilterContext" />.</param>
        /// <returns>
        ///     A <see cref="T:System.Threading.Tasks.Task" /> that on completion indicates the filter has executed.
        /// </returns>
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            Guard.NotNull(context, nameof(context));
            Guard.NotNull(context.HttpContext, nameof(context.HttpContext));
            Guard.For<ArgumentException>(() => context.HttpContext.Request == null, "Invalid action context given without any HTTP request");
            Guard.For<ArgumentException>(() => context.HttpContext.Request.Headers == null, "Invalid action context given without any HTTP request headers");
            Guard.For<ArgumentException>(() => context.HttpContext.RequestServices == null, "Invalid action context given without any HTTP request services");

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
            
            await Authorize(context, foundSecret);
        }

        /// <summary>
        /// Provides an implementation specific authorization
        /// </summary>
        /// <param name="context"></param>
        /// <param name="secretName"></param>
        /// <returns></returns>
        public abstract Task Authorize(AuthorizationFilterContext context, string secretName);
    }
}