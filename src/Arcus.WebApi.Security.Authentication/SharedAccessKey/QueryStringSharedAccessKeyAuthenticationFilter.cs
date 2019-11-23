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
    public class QueryStringSharedAccessKeyAuthenticationFilter : SharedAccessKeyAuthenticationFilter
    {
        private readonly string _parameterName, _secretName;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryStringSharedAccessKeyAuthenticationFilter"/> class.
        /// </summary>
        /// <param name="parameterName">The name of the querystring parameter which value must match the stored secret.</param>
        /// <param name="secretName">The name of the secret that's being retrieved using the <see cref="ISecretProvider.Get"/> call.</param>
        /// <exception cref="ArgumentException">When the <paramref name="parameterName"/> is <c>null</c> or blank.</exception>
        /// <exception cref="ArgumentException">When the <paramref name="secretName"/> is <c>null</c> or blank.</exception>
        public QueryStringSharedAccessKeyAuthenticationFilter(string parameterName, string secretName) : base(secretName)
        {
            Guard.NotNullOrWhitespace(parameterName, nameof(parameterName), "Header name cannot be blank");

            _parameterName = parameterName;
        }

        /// <summary>
        /// Authorizes on a given querystring parameter
        /// </summary>
        /// <param name="context"></param>
        /// <param name="secretName"></param>
        /// <returns></returns>
        public override Task Authorize(AuthorizationFilterContext context, string secretName)
        {
            if (context.HttpContext.Request.Query.ContainsKey(_parameterName))                        
            {
                if (context.HttpContext.Request.Query[_parameterName] != secretName)
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