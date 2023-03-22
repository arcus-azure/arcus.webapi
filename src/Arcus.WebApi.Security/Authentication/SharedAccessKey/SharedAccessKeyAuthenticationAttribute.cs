using System;
using Arcus.Security.Core;
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
        private readonly SharedAccessKeyAuthenticationOptions _options;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="SharedAccessKeyAuthenticationAttribute"/> class.
        /// </summary>
        /// <remarks>
        ///     Requires the Arcus secret store to retrieve the shared access key while validating the HTTP request.
        ///     For more information on the secret store, see <a href="https://security.arcus-azure.net/features/secret-store" />.
        /// </remarks>
        /// <param name="headerName">The name of the request header which value must match the stored secret.</param>
        /// <param name="queryParameterName">The name of the query parameter which value must match the stored secret.</param>
        /// <param name="secretName">The name of the secret that's being retrieved using the <see cref="ISecretProvider.GetRawSecretAsync"/> call.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="secretName"/> is blank.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="headerName"/> and the <paramref name="queryParameterName"/> are blank.</exception>
        public SharedAccessKeyAuthenticationAttribute(string secretName, string headerName = null, string queryParameterName = null) 
            : base(typeof(SharedAccessKeyAuthenticationFilter))
        {
            Guard.NotNullOrWhitespace(secretName, nameof(secretName), "Secret name cannot be blank");
            Guard.For<ArgumentException>(
                () => String.IsNullOrWhiteSpace(headerName) && String.IsNullOrWhiteSpace(queryParameterName), 
                "Requires either a non-blank header name or query parameter name");

            _options = new SharedAccessKeyAuthenticationOptions();
            Arguments = new object[] { headerName?? String.Empty, queryParameterName?? String.Empty, secretName, _options };
        }

        /// <summary>
        /// Gets or sets the flag indicating whether or not the shared access key authentication should emit security events during the authentication process of the request.
        /// </summary>
        public bool EmitSecurityEvents
        {
            get => _options.EmitSecurityEvents;
            set => _options.EmitSecurityEvents = value;
        }
    }
}
