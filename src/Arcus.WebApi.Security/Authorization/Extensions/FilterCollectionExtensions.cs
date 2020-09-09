using System;
using System.Collections.Generic;
using Arcus.WebApi.Security.Authorization;
using GuardNet;

// ReSharper disable once CheckNamespace
namespace Microsoft.AspNetCore.Mvc.Filters
{
    public static class FilterCollectionExtensions
    {
        /// <summary>
        /// Adds JWT token authorization
        /// </summary>
        /// <param name="filters">All filters that are being applied to the request pipeline</param>
        public static FilterCollection AddJwtTokenAuthorization(this FilterCollection filters)
        {
            Guard.NotNull(filters, nameof(filters));

            return filters.AddJwtTokenAuthorization(options => { });
        }

        /// <summary>
        /// Adds JWT token authorization
        /// </summary>
        /// <param name="filters">All filters that are being applied to the request pipeline</param>
        /// <param name="configureOptions">Configuration options for using JWT token authorization</param>
        public static FilterCollection AddJwtTokenAuthorization(
            this FilterCollection filters, 
            Action<JwtTokenAuthorizationOptions> configureOptions)
        {
            Guard.NotNull(filters, nameof(filters));

            var options = new JwtTokenAuthorizationOptions();
            configureOptions?.Invoke(options);
            filters.Add(new JwtTokenAuthorizationFilter(options));

            return filters;
        }

        /// <summary>
        /// Adds JWT token authorization
        /// </summary>
        /// <param name="filters">All filters that are being applied to the request pipeline</param>
        /// <param name="configureOptions">Configuration options for using JWT token authorization</param>
        /// <param name="claimCheck">Custom claims key-value pair to validate against</param>
        public static FilterCollection AddJwtTokenAuthorization(
            this FilterCollection filters,
            Action<JwtTokenAuthorizationOptions> configureOptions, IDictionary<string, string> claimCheck)
        {
            Guard.NotNull(filters, nameof(filters));
            Guard.NotAny(claimCheck, nameof(claimCheck));

            var options = new JwtTokenAuthorizationOptions(claimCheck);
            configureOptions?.Invoke(options);
            filters.Add(new JwtTokenAuthorizationFilter(options));

            return filters;
        }
    }
}