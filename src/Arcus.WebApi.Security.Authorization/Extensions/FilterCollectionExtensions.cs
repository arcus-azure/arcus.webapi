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
        /// Adds Azure Managed Identity authorization
        /// </summary>
        /// <param name="filters">All filters that are being applied to the request pipeline</param>
        /// <param name="serviceDependencies">Dependencies in the current application</param>
        public static FilterCollection AddAzureManagedIdentityAuthorization(this FilterCollection filters, IServiceCollection serviceDependencies)
        {
            Guard.NotNull(filters, nameof(filters));

            return filters.AddAzureManagedIdentityAuthorization(serviceDependencies, options => { });
        }

        /// <summary>
        /// Adds Azure Managed Identity authorization
        /// </summary>
        /// <param name="filters">All filters that are being applied to the request pipeline</param>
        /// <param name="serviceDependencies">Dependencies in the current application</param>
        /// <param name="options">Configuration options for using Azure Managed Identity authorization</param>
        public static FilterCollection AddAzureManagedIdentityAuthorization(this FilterCollection filters, IServiceCollection serviceDependencies, Action<AzureManagedIdentityAuthorizationOptions> options)
        {
            Guard.NotNull(filters, nameof(filters));

            var azureManagedIdentityAuthorizationOptions = AzureManagedIdentityAuthorizationOptions.Default;
            options(azureManagedIdentityAuthorizationOptions);

            serviceDependencies.AddTransient(services => azureManagedIdentityAuthorizationOptions);
            filters.Add<AzureManagedIdentityAuthorizationFilter>();

            return filters;
        }
    }
}
