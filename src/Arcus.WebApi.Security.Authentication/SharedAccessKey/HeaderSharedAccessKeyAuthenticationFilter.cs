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
    public class HeaderSharedAccessKeyAuthenticationFilter : SharedAccessKeyAuthenticationFilter
    {
        private readonly string _headerName, _secretName;

        /// <summary>
        /// Initializes a new instance of the <see cref="SharedAccessKeyAuthenticationFilter"/> class.
        /// </summary>
        /// <param name="headerName">The name of the request header which value must match the stored secret.</param>
        /// <param name="secretName">The name of the secret that's being retrieved using the <see cref="ISecretProvider.Get"/> call.</param>
        /// <exception cref="ArgumentException">When the <paramref name="headerName"/> is <c>null</c> or blank.</exception>
        /// <exception cref="ArgumentException">When the <paramref name="secretName"/> is <c>null</c> or blank.</exception>
        public HeaderSharedAccessKeyAuthenticationFilter(string headerName, string secretName) : base(secretName)
        {
            Guard.NotNullOrWhitespace(headerName, nameof(headerName), "Header name cannot be blank");

            _headerName = headerName;
        }

        /// <summary>
        /// Authorizes on a given header value
        /// </summary>
        /// <param name="context"></param>
        /// <param name="secretName"></param>
        /// <returns></returns>
        public override Task Authorize(AuthorizationFilterContext context, string secretName)
        {
            if (context.HttpContext.Request.Headers
                   .TryGetValue(_headerName, out StringValues requestSecretHeaders))
            {
                if (requestSecretHeaders.Any(headerValue => !String.Equals(headerValue, secretName)))
                {
                    context.Result = new UnauthorizedResult();
                }
            }
            else
            {
                context.Result = new UnauthorizedResult();
            }

            return Task.CompletedTask;
        }
    }
}