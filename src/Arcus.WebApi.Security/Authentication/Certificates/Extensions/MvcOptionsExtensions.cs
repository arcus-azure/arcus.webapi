using System;
using Arcus.WebApi.Security.Authentication.Certificates;
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
        /// <remarks>
        ///     Using this extension requires you to register an <see cref="CertificateAuthenticationValidator"/> instance yourself in the application services.
        ///     Use <see cref="AddCertificateAuthenticationFilter(MvcOptions,Action{CertificateAuthenticationConfigBuilder})"/> extension overload to configure this validator directly.
        /// </remarks>
        /// <param name="options">The current MVC options of the application.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="options"/> is <c>null</c>.</exception>
        [Obsolete("Use the " + nameof(AddCertificateAuthenticationFilter) + " overload where the certificate validation locations are configured directly")]
        public static MvcOptions AddCertificateAuthenticationFilter(this MvcOptions options)
            => AddCertificateAuthenticationFilter(options, configureOptions: null);

        /// <summary>
        /// Adds an certificate authentication MVC filter to the given <paramref name="options"/> that authenticates the incoming HTTP request.
        /// </summary>
        /// <remarks>
        ///     Using this extension requires you to register an <see cref="CertificateAuthenticationValidator"/> instance yourself in the application services.
        ///     Use <see cref="AddCertificateAuthenticationFilter(MvcOptions,Action{CertificateAuthenticationConfigBuilder})"/> extension overload to configure this validator directly.
        /// </remarks>
        /// <param name="options">The current MVC options of the application.</param>
        /// <param name="configureOptions">
        ///     The optional function to configure the set of additional consumer-configurable options to change the behavior of the certificate authentication.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="options"/> is <c>null</c>.</exception>
        [Obsolete("Use the " + nameof(AddCertificateAuthenticationFilter) + " overload where the certificate validation locations are configured directly")]
        public static MvcOptions AddCertificateAuthenticationFilter(
            this MvcOptions options,
            Action<CertificateAuthenticationOptions> configureOptions)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options), "Requires a set of MVC filters to add the certificate authentication MVC filter");
            }

            var authOptions = new CertificateAuthenticationOptions();
            configureOptions?.Invoke(authOptions);

            options.Filters.Add(new CertificateAuthenticationFilter(authOptions));
            return options;
        }

        /// <summary>
        /// Adds an certificate authentication MVC filter to the given <paramref name="options"/> that authenticates the incoming HTTP request.
        /// </summary>
        /// <remarks>
        ///     When the Arcus secret store is used during the <paramref name="configureAuthentication"/> to register the certificate locations,
        ///     make sure that the secret store is registered to avoid runtime failures.
        ///     For more information on the Arcus secret store: <a href="https://security.arcus-azure.net/features/secret-store" />.
        /// </remarks>
        /// <param name="options">The current MVC options of the application.</param>
        /// <param name="configureAuthentication">
        ///     The function to configure what information of the certificate should be used to validate the certificate during authentication.
        ///     Requires at least a single configuration location to validate the certificate.
        /// </param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when no authentication location on the certificate was configured during the <paramref name="configureAuthentication"/> function.
        /// </exception>
        public static MvcOptions AddCertificateAuthenticationFilter(
            this MvcOptions options,
            Action<CertificateAuthenticationConfigBuilder> configureAuthentication)
            => AddCertificateAuthenticationFilter(options, configureAuthentication, configureOptions: null);

        /// <summary>
        /// Adds an certificate authentication MVC filter to the given <paramref name="options"/> that authenticates the incoming HTTP request.
        /// </summary>
        /// <remarks>
        ///     When the Arcus secret store is used during the <paramref name="configureAuthentication"/> to register the certificate locations,
        ///     make sure that the secret store is registered to avoid runtime failures.
        ///     For more information on the Arcus secret store: <a href="https://security.arcus-azure.net/features/secret-store" />.
        /// </remarks>
        /// <param name="options">The current MVC options of the application.</param>
        /// <param name="configureAuthentication">
        ///     The function to configure what information of the certificate should be used to validate the certificate during authentication.
        ///     Requires at least a single configuration location to validate the certificate.
        /// </param>
        /// <param name="configureOptions">
        ///     The optional function to configure the set of additional consumer-configurable options to change the behavior of the certificate authentication.
        /// </param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="options"/> or the <paramref name="configureAuthentication"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when no authentication location on the certificate was configured during the <paramref name="configureAuthentication"/> function.
        /// </exception>
        public static MvcOptions AddCertificateAuthenticationFilter(
            this MvcOptions options,
            Action<CertificateAuthenticationConfigBuilder> configureAuthentication,
            Action<CertificateAuthenticationOptions> configureOptions)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options), "Requires a set of MVC filters to add the certificate authentication MVC filter");
            }

            if (configureAuthentication is null)
            {
                throw new ArgumentNullException(nameof(configureAuthentication), "Requires a function to configure the certificate validation locations");
            }

            var builder = new CertificateAuthenticationConfigBuilder();
            configureAuthentication(builder);
            CertificateAuthenticationConfig config = builder.Build();
            var validator = new CertificateAuthenticationValidator(config);

            var authOptions = new CertificateAuthenticationOptions();
            configureOptions?.Invoke(authOptions);

            options.Filters.Add(new CertificateAuthenticationFilter(validator, authOptions));
            return options;
        }
    }
}
