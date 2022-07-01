using System;
using System.Collections.Generic;
using System.Net.Http;
using GuardNet;
using Microsoft.Extensions.Http;

namespace Arcus.WebApi.Tests.Unit.Logging.Fixture
{
    /// <summary>
    /// Represents a builder instance that combines many <see cref="HttpMessageHandler"/> instances.
    /// </summary>
    public class DefaultHttpMessageHandlerBuilder : HttpMessageHandlerBuilder
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultHttpMessageHandlerBuilder" /> class.
        /// </summary>
        /// <param name="provider">The application services available for the application.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="provider"/> is <c>null</c>.</exception>
        public DefaultHttpMessageHandlerBuilder(IServiceProvider provider)
        {
            Guard.NotNull(provider, nameof(provider), "Requires a service provider to retrieve the available application services when registering HTTP message handlers");
            Services = provider;
        }

        /// <summary>
        /// Gets an <see cref="T:System.IServiceProvider" /> which can be used to resolve services
        /// from the dependency injection container.
        /// </summary>
        /// <remarks>
        /// This property is sensitive to the value of
        /// <see cref="P:Microsoft.Extensions.Http.HttpClientFactoryOptions.SuppressHandlerScope" />. If <c>true</c> this
        /// property will be a reference to the application's root service provider. If <c>false</c>
        /// (default) this will be a reference to a scoped service provider that has the same
        /// lifetime as the handler being created.
        /// </remarks>
        public override IServiceProvider Services { get; }

        /// <summary>
        /// Creates an <see cref="T:System.Net.Http.HttpMessageHandler" />.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Net.Http.HttpMessageHandler" /> built from the <see cref="P:Microsoft.Extensions.Http.HttpMessageHandlerBuilder.PrimaryHandler" /> and
        /// <see cref="P:Microsoft.Extensions.Http.HttpMessageHandlerBuilder.AdditionalHandlers" />.
        /// </returns>
        public override HttpMessageHandler Build()
        {
            return CreateHandlerPipeline(PrimaryHandler, AdditionalHandlers);
        }

        /// <summary>
        /// Gets a list of additional <see cref="T:System.Net.Http.DelegatingHandler" /> instances used to configure an
        /// <see cref="T:System.Net.Http.HttpClient" /> pipeline.
        /// </summary>
        public override IList<DelegatingHandler> AdditionalHandlers { get; } = new List<DelegatingHandler>();

        /// <summary>
        /// Gets or sets the name of the <see cref="T:System.Net.Http.HttpClient" /> being created.
        /// </summary>
        /// <remarks>
        /// The <see cref="P:Microsoft.Extensions.Http.HttpMessageHandlerBuilder.Name" /> is set by the <see cref="T:System.Net.Http.IHttpClientFactory" /> infrastructure
        /// and is public for unit testing purposes only. Setting the <see cref="P:Microsoft.Extensions.Http.HttpMessageHandlerBuilder.Name" /> outside of
        /// testing scenarios may have unpredictable results.
        /// </remarks>
        public override string Name { get; set; }

        /// <summary>
        /// Gets or sets the primary <see cref="T:System.Net.Http.HttpMessageHandler" />.
        /// </summary>
        public override HttpMessageHandler PrimaryHandler { get; set; }
    }
}