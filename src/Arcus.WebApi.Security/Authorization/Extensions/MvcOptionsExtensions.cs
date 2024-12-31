using System;
using System.Collections.Generic;
using System.Linq;
using Arcus.WebApi.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions on the <see cref="MvcOptions"/> related to authorization.
    /// </summary>
    public static partial class MvcOptionsExtensions
    {
        /// <summary>
        /// Adds JWT token authorization.
        /// </summary>
        /// <param name="options">The options that are being applied to the request pipeline.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="options"/> is <c>null</c>.</exception>
        public static MvcOptions AddJwtTokenAuthorizationFilter(this MvcOptions options)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options), "Requires a filter collection to add the JWT token authorization filter");
            }

            return AddJwtTokenAuthorizationFilter(options, configureOptions: null);
        }

        /// <summary>
        /// Adds JWT token authorization.
        /// </summary>
        /// <param name="options">The options that are being applied to the request pipeline/</param>
        /// <param name="configureOptions">The configuration options for using JWT token authorization.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="options"/> is <c>null</c>.</exception>
        public static MvcOptions AddJwtTokenAuthorizationFilter(
            this MvcOptions options, 
            Action<JwtTokenAuthorizationOptions> configureOptions)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options), "Requires a filter collection to add the JWT token authorization filter");
            }

            var authOptions= new JwtTokenAuthorizationOptions();
            configureOptions?.Invoke(authOptions);
            options.Filters.Add(new JwtTokenAuthorizationFilter(authOptions));

            return options;
        }
        /// <summary>
        /// Adds JWT token authorization.
        /// </summary>
        /// <param name="options">The options that are being applied to the request pipeline.</param>
        /// <param name="claimCheck">The custom claims key-value pair to validate against.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="claimCheck"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="claimCheck"/> doesn't have any entries or one of the entries has blank key/value inputs.</exception>
        public static MvcOptions AddJwtTokenAuthorizationFilter(
            this MvcOptions options,
            IDictionary<string, string> claimCheck)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options), "Requires a filter collection to add the JWT token authorization filter");
            }
            if (claimCheck is null)
            {
                throw new ArgumentNullException(nameof(claimCheck), "Requires a set of claim checks to verify the claims request JWT");
            }
            if (!claimCheck.Any())
            {
                throw new ArgumentException("Requires at least one entry in the set of claim checks to verify the claims in the request JWT", nameof(claimCheck));
            }
            if (claimCheck.Any(item => string.IsNullOrWhiteSpace(item.Key) || string.IsNullOrWhiteSpace(item.Value)))
            {
                throw new ArgumentException("Requires all entries in the set of claim checks to be non-blank to correctly verify the claims in the request JWT");
            }

            return AddJwtTokenAuthorizationFilter(options, configureOptions: null, claimCheck: claimCheck);
        }

        /// <summary>
        /// Adds JWT token authorization.
        /// </summary>
        /// <param name="options">The options that are being applied to the request pipeline.</param>
        /// <param name="configureOptions">The configuration options for using JWT token authorization.</param>
        /// <param name="claimCheck">The custom claims key-value pair to validate against.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="claimCheck"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="claimCheck"/> doesn't have any entries or one of the entries has blank key/value inputs.</exception>
        public static MvcOptions AddJwtTokenAuthorizationFilter(
            this MvcOptions options,
            Action<JwtTokenAuthorizationOptions> configureOptions, 
            IDictionary<string, string> claimCheck)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options), "Requires a filter collection to add the JWT token authorization filter");
            }
            if (claimCheck is null)
            {
                throw new ArgumentNullException(nameof(claimCheck), "Requires a set of claim checks to verify the claims request JWT");
            }
            if (!claimCheck.Any())
            {
                throw new ArgumentException("Requires at least one entry in the set of claim checks to verify the claims in the request JWT", nameof(claimCheck));
            }
            if (claimCheck.Any(item => string.IsNullOrWhiteSpace(item.Key) || string.IsNullOrWhiteSpace(item.Value)))
            {
                throw new ArgumentException("Requires all entries in the set of claim checks to be non-blank to correctly verify the claims in the request JWT");
            }

            var authOptions = new JwtTokenAuthorizationOptions(claimCheck);
            configureOptions?.Invoke(authOptions);
            options.Filters.Add(new JwtTokenAuthorizationFilter(authOptions));

            return options;
        }
    }
}
