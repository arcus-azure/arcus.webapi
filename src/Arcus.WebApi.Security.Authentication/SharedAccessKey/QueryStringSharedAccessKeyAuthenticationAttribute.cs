using System;
using Arcus.Security.Secrets.Core.Interfaces;
using GuardNet;
using Microsoft.AspNetCore.Mvc;

namespace Arcus.WebApi.Security.Authentication.SharedAccessKey
{
    /// <summary>
    /// Authentication filter to secure HTTP requests with shared access keys.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)] 
    public class QueryStringSharedAccessKeyAuthenticationAttribute : TypeFilterAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QueryStringSharedAccessKeyAuthenticationAttribute"/> class.
        /// </summary>
        /// <param name="parameterName">The name of the querystring parameter which value must match the stored secret with the same name as the parameter.</param>
        /// <exception cref="ArgumentException">When the <paramref name="parameterName"/> is <c>null</c> or blank.</exception>
        public QueryStringSharedAccessKeyAuthenticationAttribute(string parameterName) : this(parameterName, parameterName) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryStringSharedAccessKeyAuthenticationAttribute"/> class.
        /// </summary>
        /// <param name="parameterName">The name of the request header which value must match the stored secret.</param>
        /// <param name="secretName">The name of the secret that's being retrieved using the <see cref="ISecretProvider.Get"/> call.</param>
        /// <exception cref="ArgumentException">When the <paramref name="parameterName"/> is <c>null</c> or blank.</exception>
        /// <exception cref="ArgumentException">When the <paramref name="secretName"/> is <c>null</c> or blank.</exception>
        public QueryStringSharedAccessKeyAuthenticationAttribute(string parameterName, string secretName) : base(typeof(QueryStringSharedAccessKeyAuthenticationFilter))
        {
            Guard.NotNullOrWhitespace(parameterName, nameof(parameterName), "Parameter name cannot be blank");
            Guard.NotNullOrWhitespace(secretName, nameof(secretName), "Secret name cannot be blank");

            Arguments = new object[] { parameterName, secretName };
        }
    }
}
