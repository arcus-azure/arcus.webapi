using System;
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
    }
}