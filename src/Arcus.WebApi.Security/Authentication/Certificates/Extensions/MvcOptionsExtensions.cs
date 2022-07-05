using System;
using Arcus.WebApi.Security.Authentication.Certificates;
using GuardNet;
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
        /// Adds an certificate authentication MVC filter to the given <paramref name="options"/> that authenticates the incoming HTTP request.
        /// </summary>
        /// <param name="options">The current MVC options of the application.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="options"/> is <c>null</c>.</exception>
        public static MvcOptions AddCertificateAuthenticationFilter(this MvcOptions options)
        {
            Guard.NotNull(options, nameof(options), "Requires a set of MVC filters to add the certificate authentication MVC filter");

            return AddCertificateAuthenticationFilter(options, configureOptions: null);
        }
        
        /// <summary>
        /// Adds an certificate authentication MVC filter to the given <paramref name="options"/> that authenticates the incoming HTTP request.
        /// </summary>
        /// <param name="options">The current MVC options of the application.</param>
        /// <param name="configureOptions">
        ///     The optional function to configure the set of additional consumer-configurable options to change the behavior of the certificate authentication.
        /// </param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="options"/> is <c>null</c>.</exception>
        public static MvcOptions AddCertificateAuthenticationFilter(
            this MvcOptions options,
            Action<CertificateAuthenticationOptions> configureOptions)
        {
            Guard.NotNull(options, nameof(options), "Requires a set of MVC filters to add the certificate authentication MVC filter");

            var authOptions = new CertificateAuthenticationOptions();
            configureOptions?.Invoke(authOptions);

            options.Filters.Add(new CertificateAuthenticationFilter(authOptions));
            return options;
        }
    }
}
