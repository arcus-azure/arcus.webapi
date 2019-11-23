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
    public class SharedAccessKeyAuthenticationAttribute : TypeFilterAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SharedAccessKeyAuthenticationAttribute"/> class.
        /// </summary>
        /// <param name="secretName">The name of the request header which value must match the stored secret with the same name as the header.</param>
        /// <exception cref="ArgumentException">When the <paramref name="secretName"/> is <c>null</c> or blank.</exception>
        public SharedAccessKeyAuthenticationAttribute(string secretName) : this(secretName, secretName) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SharedAccessKeyAuthenticationAttribute"/> class.
        /// </summary>
        /// <param name="headerName">The name of the request header which value must match the stored secret.</param>
        /// <param name="secretName">The name of the secret that's being retrieved using the <see cref="ISecretProvider.Get"/> call.</param>
        /// <exception cref="ArgumentException">When the <paramref name="headerName"/> is <c>null</c> or blank.</exception>
        /// <exception cref="ArgumentException">When the <paramref name="secretName"/> is <c>null</c> or blank.</exception>
        public SharedAccessKeyAuthenticationAttribute(string headerName, string secretName) : base(typeof(HeaderSharedAccessKeyAuthenticationFilter))
        {
            Guard.NotNullOrWhitespace(headerName, nameof(headerName), "Header name cannot be blank");
            Guard.NotNullOrWhitespace(secretName, nameof(secretName), "Secret name cannot be blank");

            Arguments = new object[] { headerName, secretName };
        }
    }
}
