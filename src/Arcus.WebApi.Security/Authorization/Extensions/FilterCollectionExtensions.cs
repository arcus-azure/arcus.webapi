using System;
using Arcus.WebApi.Security.Authorization;
using GuardNet;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Microsoft.AspNetCore.Mvc.Filters
{
    public static class FilterCollectionExtensions
    {
        /// <summary>
        /// Adds JWT token authorization
        /// </summary>
        /// <param name="filters">All filters that are being applied to the request pipeline</param>
        /// <param name="serviceDependencies">Dependencies in the current application</param>
        public static FilterCollection AddJwtTokenAuthorization(this FilterCollection filters, IServiceCollection serviceDependencies)
        {
            Guard.NotNull(filters, nameof(filters));

            return filters.AddJwtTokenAuthorization(serviceDependencies, options => { });
        }

        /// <summary>
        /// Adds JWT token authorization
        /// </summary>
        /// <param name="filters">All filters that are being applied to the request pipeline</param>
        /// <param name="serviceDependencies">Dependencies in the current application</param>
        /// <param name="options">Configuration options for using JWT token authorization</param>
        public static FilterCollection AddJwtTokenAuthorization(this FilterCollection filters, IServiceCollection serviceDependencies, Action<JwtTokenAuthorizationOptions> options)
        {
            Guard.NotNull(filters, nameof(filters));
            
            serviceDependencies.Configure<JwtTokenAuthorizationOptions>(options);
            filters.Add<JwtTokenAuthorizationFilter>();

            return filters;
        }
    }
}