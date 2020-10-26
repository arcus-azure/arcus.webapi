using System;
using System.Collections.Generic;
using System.Linq;
using Arcus.WebApi.Security.Authorization;
using GuardNet;

// ReSharper disable once CheckNamespace
namespace Microsoft.AspNetCore.Mvc.Filters
{
    /// <summary>
    /// Extensions on the <see cref="FilterCollection"/> related to authorization.
    /// </summary>
    public static class FilterCollectionExtensions
    {
        /// <summary>
        /// Adds JWT token authorization to the MVC <see cref="FilterCollection"/>.
        /// </summary>
        /// <param name="filters">All filters that are being applied to the request pipeline</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="filters"/> is <c>null</c>.</exception>
        public static FilterCollection AddJwtTokenAuthorization(this FilterCollection filters)
        {
            Guard.NotNull(filters, nameof(filters), "Requires a filter collection to add the JWT token authorization filter");

            return filters.AddJwtTokenAuthorization(configureOptions: null);
        }

        /// <summary>
        /// Adds JWT token authorization to the MVC <see cref="FilterCollection"/>.
        /// </summary>
        /// <param name="filters">All filters that are being applied to the request pipeline</param>
        /// <param name="configureOptions">Configuration options for using JWT token authorization</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="filters"/> is <c>null</c>.</exception>
        public static FilterCollection AddJwtTokenAuthorization(
            this FilterCollection filters, 
            Action<JwtTokenAuthorizationOptions> configureOptions)
        {
            Guard.NotNull(filters, nameof(filters), "Requires a filter collection to add the JWT token authorization filter");

            var options = new JwtTokenAuthorizationOptions();
            configureOptions?.Invoke(options);
            filters.Add(new JwtTokenAuthorizationFilter(options));

            return filters;
        }
        /// <summary>
        /// Adds JWT token authorization
        /// </summary>
        /// <param name="filters">All filters that are being applied to the request pipeline</param>
        /// <param name="claimCheck">Custom claims key-value pair to validate against</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="claimCheck"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="claimCheck"/> doesn't have any entries or one of the entries has blank key/value inputs.</exception>
        public static FilterCollection AddJwtTokenAuthorization(
            this FilterCollection filters,
            IDictionary<string, string> claimCheck)
        {
            Guard.NotNull(filters, nameof(filters), "Requires a filter collection to add the JWT token authorization filter");
            Guard.NotNull(claimCheck, nameof(claimCheck), "Requires a set of claim checks to verify the claims request JWT");
            Guard.NotAny(claimCheck, nameof(claimCheck), "Requires at least one entry in the set of claim checks to verify the claims in the request JWT");
            Guard.For<ArgumentException>(() => claimCheck.Any(item => String.IsNullOrWhiteSpace(item.Key) || String.IsNullOrWhiteSpace(item.Value)), 
                "Requires all entries in the set of claim checks to be non-blank to correctly verify the claims in the request JWT");

            AddJwtTokenAuthorization(filters, configureOptions: null, claimCheck: claimCheck);

            return filters;
        }

        /// <summary>
        /// Adds JWT token authorization
        /// </summary>
        /// <param name="filters">All filters that are being applied to the request pipeline</param>
        /// <param name="configureOptions">Configuration options for using JWT token authorization</param>
        /// <param name="claimCheck">Custom claims key-value pair to validate against</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="claimCheck"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="claimCheck"/> doesn't have any entries or one of the entries has blank key/value inputs.</exception>
        public static FilterCollection AddJwtTokenAuthorization(
            this FilterCollection filters,
            Action<JwtTokenAuthorizationOptions> configureOptions, IDictionary<string, string> claimCheck)
        {
            Guard.NotNull(filters, nameof(filters), "Requires a filter collection to add the JWT token authorization filter");
            Guard.NotNull(claimCheck, nameof(claimCheck), "Requires a set of claim checks to verify the claims request JWT");
            Guard.NotAny(claimCheck, nameof(claimCheck), "Requires at least one entry in the set of claim checks to verify the claims in the request JWT");
            Guard.For<ArgumentException>(() => claimCheck.Any(item => String.IsNullOrWhiteSpace(item.Key) || String.IsNullOrWhiteSpace(item.Value)), 
                "Requires all entries in the set of claim checks to be non-blank to correctly verify the claims in the request JWT");

            var options = new JwtTokenAuthorizationOptions(claimCheck);
            configureOptions?.Invoke(options);
            filters.Add(new JwtTokenAuthorizationFilter(options));

            return filters;
        }
    }
}