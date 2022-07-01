﻿using System;
using Arcus.WebApi.Logging.Core.Correlation;
using GuardNet;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions on the <see cref="IHttpClientBuilder"/> for more easily HTTP correlation additions.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class IHttpClientBuilderExtensions
    {
        /// <summary>
        /// Adds an additional HTTP message handler that will enrich the send HTTP request with HTTP correlation.
        /// </summary>
        /// <param name="builder">The builder instance to add the HTTP message handler.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="builder"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the no <see cref="IHttpContextAccessor"/> was found in the dependency injection container.</exception>
        public static IHttpClientBuilder WithHttpCorrelationTracking(this IHttpClientBuilder builder)
        {
            Guard.NotNull(builder, nameof(builder), "Requires a HTTP client builder instance to add the HTTP correlation message handler");
            return WithHttpCorrelationTracking(builder, configureOptions: null);
        }

        /// <summary>
        /// Adds an additional HTTP message handler that will enrich the send HTTP request with HTTP correlation.
        /// </summary>
        /// <param name="builder">The builder instance to add the HTTP message handler.</param>
        /// <param name="configureOptions">The function to configure additional options that influence how the HTTP correlation will be added and tracked.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="builder"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the no <see cref="IHttpContextAccessor"/> was found in the dependency injection container.</exception>
        public static IHttpClientBuilder WithHttpCorrelationTracking(this IHttpClientBuilder builder, Action<HttpCorrelationClientOptions> configureOptions)
        {
            Guard.NotNull(builder, nameof(builder), "Requires a HTTP client builder instance to add the HTTP correlation message handler");

            return builder.AddHttpMessageHandler(serviceProvider =>
            {
                var correlationAccessor = serviceProvider.GetService<IHttpCorrelationInfoAccessor>();
                if (correlationAccessor is null)
                {
                    throw new InvalidOperationException(
                        );
                }

                var options = new HttpCorrelationClientOptions();
                configureOptions?.Invoke(options);

                var logger = serviceProvider.GetRequiredService<ILogger<HttpCorrelationMessageHandler>>();
                return new HttpCorrelationMessageHandler(correlationAccessor, options, logger);
            });
        }
    }
}
