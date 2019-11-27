using System;
using Correlate;
using GuardNet;

namespace Arcus.WebApi.Correlation 
{
    /// <summary>
    /// Factory to create/clean up a <see cref="Correlation"/> and associate it with a <see cref="ICorrelationContextAccessor"/>.
    /// </summary>
    internal class CorrelationFactory : CorrelationContextFactory
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CorrelationFactory"/> class.
        /// </summary>
        /// <param name="accessor">The accessor implementation to associate the <see cref="Correlation"/> with.</param>
        public CorrelationFactory(ICorrelationContextAccessor accessor) : base(accessor)
        {
            Guard.NotNull(accessor, nameof(accessor));
        }

        /// <summary>
        /// Creates a new <see cref="T:Correlate.CorrelationContext" />.
        /// </summary>
        /// <param name="correlationId">The correlation id to associate to the context.</param>
        /// <returns>The <see cref="T:Correlate.CorrelationContext" /> containing the correlation id.</returns>
        public override CorrelationContext Create(string correlationId)
        {
            Guard.NotNullOrWhitespace(correlationId, nameof(correlationId), "Cannot create correlation ID from blank text value");

            return new Correlation { CorrelationId = correlationId };
        }
    }
}