using System;
using Arcus.Security.Core;
using Arcus.WebApi.Security.Authentication.SharedAccessKey;
using GuardNet;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Microsoft.AspNetCore.Mvc.Filters
{
    /// <summary>
    /// Extensions on the <see cref="FilterCollection"/> related to authentication.
    /// </summary>
    public static partial class FilterCollectionExtensions
    {
        /// <summary>
        /// Adds an shared access key authentication MVC filter to the given <paramref name="filters"/> that authenticates the incoming HTTP request on its header.
        /// </summary>
        /// <param name="filters">The current MVC filters of the application.</param>
        /// <param name="headerName">The name of the request header which value must match the stored secret.</param>
        /// <param name="secretName">The name of the secret that's being retrieved using the <see cref="ISecretProvider.GetRawSecretAsync"/> call.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="filters"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="headerName"/> or <paramref name="secretName"/> is blank.</exception>
        [Obsolete("Use the " + nameof(MvcOptionsExtensions.AddSharedAccessKeyAuthenticationFilterOnHeader) + " instead via services.AddControllers(options => options." + nameof(MvcOptionsExtensions.AddSharedAccessKeyAuthenticationFilterOnHeader) + "(...))")]
        public static FilterCollection AddSharedAccessKeyAuthenticationOnHeader(
            this FilterCollection filters,
            string headerName,
            string secretName)
        {
            Guard.NotNull(filters, nameof(filters), "Requires a set of MVC filters to add the shared access authentication MVC filter");
            Guard.NotNullOrWhitespace(headerName, nameof(headerName), "Requires a non-blank HTTP request header name to match the stored secret during the shared access key authentication");

            return AddSharedAccessKeyAuthenticationOnHeader(filters, headerName, secretName, configureOptions: null);
        }

        /// <summary>
        /// Adds an shared access key authentication MVC filter to the given <paramref name="filters"/> that authenticates the incoming HTTP request on its header.
        /// </summary>
        /// <param name="filters">The current MVC filters of the application.</param>
        /// <param name="headerName">The name of the request header which value must match the stored secret.</param>
        /// <param name="secretName">The name of the secret that's being retrieved using the <see cref="ISecretProvider.GetRawSecretAsync"/> call.</param>
        /// <param name="configureOptions">
        ///     The optional function to configure the set of additional consumer-configurable options to change the behavior of the shared access authentication.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="filters"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="headerName"/> or <paramref name="secretName"/> is blank.</exception>
         [Obsolete("Use the " + nameof(MvcOptionsExtensions.AddSharedAccessKeyAuthenticationFilterOnHeader) + "instead via services.AddControllers(options => options." + nameof(MvcOptionsExtensions.AddSharedAccessKeyAuthenticationFilterOnHeader) + "(...))")]
        public static FilterCollection AddSharedAccessKeyAuthenticationOnHeader(
            this FilterCollection filters, 
            string headerName,
            string secretName,
            Action<SharedAccessKeyAuthenticationOptions> configureOptions)
        {
            Guard.NotNull(filters, nameof(filters), "Requires a set of MVC filters to add the shared access authentication MVC filter");
            Guard.NotNullOrWhitespace(headerName, nameof(headerName), "Requires a non-blank HTTP request header name to match the stored secret during the shared access key authentication");
            Guard.NotNullOrWhitespace(secretName, nameof(secretName), "Requires a non-blank secret name to retrieve the stored access key in the secret store during the shared access key authentication");
            
            var options = new SharedAccessKeyAuthenticationOptions();
            configureOptions?.Invoke(options);
            
            filters.Add(new SharedAccessKeyAuthenticationFilter(headerName, queryParameterName: null, secretName, options));
            return filters;
        }

        /// <summary>
        /// Adds an shared access key authentication MVC filter to the given <paramref name="filters"/> that authenticates the incoming HTTP request on its query.
        /// </summary>
        /// <param name="filters">The current MVC filters of the application.</param>
        /// <param name="parameterName">The name of the request query parameter name which value must match the stored secret.</param>
        /// <param name="secretName">The name of the secret that's being retrieved using the <see cref="ISecretProvider.GetRawSecretAsync"/> call.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="filters"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="parameterName"/> or <paramref name="secretName"/> is blank.</exception>
        [Obsolete("Use the " + nameof(MvcOptionsExtensions.AddSharedAccessKeyAuthenticationFilterOnQuery) + "instead via services.AddControllers(options => options." + nameof(MvcOptionsExtensions.AddSharedAccessKeyAuthenticationFilterOnQuery) + "(...))")]
        public static FilterCollection AddSharedAccessAuthenticationOnQuery(
            this FilterCollection filters,
            string parameterName,
            string secretName)
        {
            Guard.NotNull(filters, nameof(filters), "Requires a set of MVC filters to add the shared access authentication MVC filter");
            Guard.NotNullOrWhitespace(parameterName, nameof(parameterName), "Requires a non-blank HTTP request query parameter name name to match the stored secret during the shared access key authentication");
            Guard.NotNullOrWhitespace(secretName, nameof(secretName), "Requires a non-blank secret name to retrieve the stored access key in the secret store during the shared access key authentication");

            return AddSharedAccessAuthenticationOnQuery(filters, parameterName, secretName, configureOptions: null);
        }

        /// <summary>
        /// Adds an shared access key authentication MVC filter to the given <paramref name="filters"/> that authenticates the incoming HTTP request on its query.
        /// </summary>
        /// <param name="filters">The current MVC filters of the application.</param>
        /// <param name="parameterName">The name of the request query parameter name which value must match the stored secret.</param>
        /// <param name="secretName">The name of the secret that's being retrieved using the <see cref="ISecretProvider.GetRawSecretAsync"/> call.</param>
        /// <param name="configureOptions">
        ///     The optional function to configure the set of additional consumer-configurable options to change the behavior of the shared access authentication.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="filters"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="parameterName"/> or <paramref name="secretName"/> is blank.</exception>
        [Obsolete("Use the " + nameof(MvcOptionsExtensions.AddSharedAccessKeyAuthenticationFilterOnQuery) + "instead via services.AddControllers(options => options." + nameof(MvcOptionsExtensions.AddSharedAccessKeyAuthenticationFilterOnQuery) + "(...))")]
        public static FilterCollection AddSharedAccessAuthenticationOnQuery(
            this FilterCollection filters, 
            string parameterName,
            string secretName,
            Action<SharedAccessKeyAuthenticationOptions> configureOptions)
        {
            Guard.NotNull(filters, nameof(filters), "Requires a set of MVC filters to add the shared access authentication MVC filter");
            Guard.NotNullOrWhitespace(parameterName, nameof(parameterName), "Requires a non-blank HTTP request query parameter name name to match the stored secret during the shared access key authentication");
            Guard.NotNullOrWhitespace(secretName, nameof(secretName), "Requires a non-blank secret name to retrieve the stored access key in the secret store during the shared access key authentication");
            
            var options = new SharedAccessKeyAuthenticationOptions();
            configureOptions?.Invoke(options);
            
            filters.Add(new SharedAccessKeyAuthenticationFilter(headerName: null, queryParameterName: parameterName, secretName, options));
            return filters;
        }
    }
}
