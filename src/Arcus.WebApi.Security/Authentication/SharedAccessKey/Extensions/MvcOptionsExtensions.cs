using System;
using Arcus.Security.Core;
using Arcus.WebApi.Security.Authentication.SharedAccessKey;
using Microsoft.AspNetCore.Mvc;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions on the <see cref="MvcOptions"/> related to authentication.
    /// </summary>
    public static partial class MvcOptionsExtensions
    {
        /// <summary>
        /// Adds an shared access key authentication MVC filter to the given <paramref name="options"/> that authenticates the incoming HTTP request on its header.
        /// </summary>
        /// <remarks>
        ///     Requires the Arcus secret store to retrieve the shared access key while validating the HTTP request.
        ///     For more information on the secret store, see <a href="https://security.arcus-azure.net/features/secret-store" />.
        /// </remarks>
        /// <param name="options">The current MVC options of the application.</param>
        /// <param name="headerName">The name of the request header which value must match the stored secret.</param>
        /// <param name="secretName">The name of the secret that's being retrieved using the <see cref="ISecretProvider.GetRawSecretAsync"/> call.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="headerName"/> or <paramref name="secretName"/> is blank.</exception>
        public static MvcOptions AddSharedAccessKeyAuthenticationFilterOnHeader(this MvcOptions options, string headerName, string secretName)
            => AddSharedAccessKeyAuthenticationFilterOnHeader(options, headerName, secretName, configureOptions: null);

        /// <summary>
        /// Adds an shared access key authentication MVC filter to the given <paramref name="options"/> that authenticates the incoming HTTP request on its header.
        /// </summary>
        /// <remarks>
        ///     Requires the Arcus secret store to retrieve the shared access key while validating the HTTP request.
        ///     For more information on the secret store, see <a href="https://security.arcus-azure.net/features/secret-store" />.
        /// </remarks>
        /// <param name="options">The current MVC options of the application.</param>
        /// <param name="headerName">The name of the request header which value must match the stored secret.</param>
        /// <param name="secretName">The name of the secret that's being retrieved using the <see cref="ISecretProvider.GetRawSecretAsync"/> call.</param>
        /// <param name="configureOptions">
        ///     The optional function to configure the set of additional consumer-configurable options to change the behavior of the shared access authentication.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="headerName"/> or <paramref name="secretName"/> is blank.</exception>
        public static MvcOptions AddSharedAccessKeyAuthenticationFilterOnHeader(
            this MvcOptions options, 
            string headerName, 
            string secretName, 
            Action<SharedAccessKeyAuthenticationOptions> configureOptions)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options), "Requires a MVC options instance to add the shared access key authentication filter");
            }
            if (string.IsNullOrWhiteSpace(headerName))
            {
                throw new ArgumentException("Requires a non-blank HTTP request header name to match the stored secret during the shared access key authentication", nameof(headerName));
            }

            var authOptions = new SharedAccessKeyAuthenticationOptions();
            configureOptions?.Invoke(authOptions);
            
            options.Filters.Add(new SharedAccessKeyAuthenticationFilter(headerName, queryParameterName: null, secretName, authOptions));
            return options;
        }

        /// <summary>
        /// Adds an shared access key authentication MVC filter to the given <paramref name="options"/> that authenticates the incoming HTTP request on its query.
        /// </summary>
        /// <remarks>
        ///     Requires the Arcus secret store to retrieve the shared access key while validating the HTTP request.
        ///     For more information on the secret store, see <a href="https://security.arcus-azure.net/features/secret-store" />.
        /// </remarks>
        /// <param name="options">The current MVC options of the application.</param>
        /// <param name="parameterName">The name of the request query parameter name which value must match the stored secret.</param>
        /// <param name="secretName">The name of the secret that's being retrieved using the <see cref="ISecretProvider.GetRawSecretAsync"/> call.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="parameterName"/> or <paramref name="secretName"/> is blank.</exception>
        public static MvcOptions AddSharedAccessKeyAuthenticationFilterOnQuery(
            this MvcOptions options,
            string parameterName,
            string secretName)
            => AddSharedAccessKeyAuthenticationFilterOnQuery(options, parameterName, secretName, configureOptions: null);

        /// <summary>
        /// Adds an shared access key authentication MVC filter to the given <paramref name="options"/> that authenticates the incoming HTTP request on its query.
        /// </summary>
        /// <remarks>
        ///     Requires the Arcus secret store to retrieve the shared access key while validating the HTTP request.
        ///     For more information on the secret store, see <a href="https://security.arcus-azure.net/features/secret-store" />.
        /// </remarks>
        /// <param name="options">The current MVC options of the application.</param>
        /// <param name="parameterName">The name of the request query parameter name which value must match the stored secret.</param>
        /// <param name="secretName">The name of the secret that's being retrieved using the <see cref="ISecretProvider.GetRawSecretAsync"/> call.</param>
        /// <param name="configureOptions">
        ///     The optional function to configure the set of additional consumer-configurable options to change the behavior of the shared access authentication.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="parameterName"/> or <paramref name="secretName"/> is blank.</exception>
        public static MvcOptions AddSharedAccessKeyAuthenticationFilterOnQuery(
            this MvcOptions options, 
            string parameterName,
            string secretName,
            Action<SharedAccessKeyAuthenticationOptions> configureOptions)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options), "Requires a set of MVC options to add the shared access authentication MVC filter");
            }

            if (string.IsNullOrWhiteSpace(parameterName))
            {
                throw new ArgumentException("Requires a non-blank HTTP request query parameter name to match the stored secret during the shared access key authentication", nameof(parameterName));
            }

            if (string.IsNullOrWhiteSpace(secretName))
            {
                throw new ArgumentException("Requires a non-blank secret name to retrieve the stored access key in the secret store during the shared access key authentication", nameof(secretName));
            }
            
            var authOptions = new SharedAccessKeyAuthenticationOptions();
            configureOptions?.Invoke(authOptions);
            
            options.Filters.Add(new SharedAccessKeyAuthenticationFilter(headerName: null, queryParameterName: parameterName, secretName, authOptions));
            return options;
        }
    }
}
