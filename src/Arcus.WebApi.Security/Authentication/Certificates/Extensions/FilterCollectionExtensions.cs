using System;
using System.Diagnostics.CodeAnalysis;
using Arcus.WebApi.Security.Authentication.Certificates;
using GuardNet;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Microsoft.AspNetCore.Mvc.Filters
{
    /// <summary>
    /// Extensions on the <see cref="FilterCollection"/> related to authentication.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static partial class FilterCollectionExtensions
    {
        /// <summary>
        /// Adds an certificate authentication MVC filter to the given <paramref name="filters"/> that authenticates the incoming HTTP request.
        /// </summary>
        /// <param name="filters">The current MVC filters of the application.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="filters"/> is <c>null</c>.</exception>
        [Obsolete("Use the " + nameof(MvcOptionsExtensions.AddCertificateAuthenticationFilter) + " instead via the services.AddControllers(options => options." + nameof(MvcOptionsExtensions.AddCertificateAuthenticationFilter) + "())")]
        public static FilterCollection AddCertificateAuthentication(this FilterCollection filters)
        {
            Guard.NotNull(filters, nameof(filters), "Requires a set of MVC filters to add the certificate authentication MVC filter");

            return AddCertificateAuthentication(filters, configureOptions: null);
        }

        /// <summary>
        /// Adds an certificate authentication MVC filter to the given <paramref name="filters"/> that authenticates the incoming HTTP request.
        /// </summary>
        /// <param name="filters">The current MVC filters of the application.</param>
        /// <param name="configureOptions">
        ///     The optional function to configure the set of additional consumer-configurable options to change the behavior of the certificate authentication.
        /// </param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="filters"/> is <c>null</c>.</exception>
        [Obsolete("Use the " + nameof(MvcOptionsExtensions.AddCertificateAuthenticationFilter) + " instead via the services.AddControllers(options => options." + nameof(MvcOptionsExtensions.AddCertificateAuthenticationFilter) + "(...))")]
        public static FilterCollection AddCertificateAuthentication(
            this FilterCollection filters,
            Action<CertificateAuthenticationOptions> configureOptions)
        {
            Guard.NotNull(filters, nameof(filters), "Requires a set of MVC filters to add the certificate authentication MVC filter");

            var options = new CertificateAuthenticationOptions();
            configureOptions?.Invoke(options);

            filters.Add(new CertificateAuthenticationFilter(options));
            return filters;
        }
    }
}
