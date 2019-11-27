using Correlate;
using GuardNet;

namespace Arcus.WebApi.Correlation 
{
    /// <summary>
    /// Represents a singleton <see cref="ICorrelationAccessor"/> implementation to store the correlation ID information.
    /// </summary>
    public class CorrelationAccessor : ICorrelationAccessor
    {
        private readonly ICorrelationContextAccessor _accessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="CorrelationAccessor"/> class.
        /// </summary>
        public CorrelationAccessor(ICorrelationContextAccessor accessor)
        {
            Guard.NotNull(accessor, nameof(accessor));

            _accessor = accessor;
        }

        private Correlation Correlation => _accessor.CorrelationContext as Correlation;

        /// <summary>
        /// Gets the transactional ID of the request.
        /// </summary>
        public string CorrelationId => Correlation.CorrelationId;

        /// <summary>
        /// Gets the unique operation ID of the request.
        /// </summary>
        public string RequestId => Correlation.RequestId;

        /// <summary>
        /// Gets or sets <see cref="ICorrelationContextAccessor.CorrelationContext" />.
        /// </summary>
        public CorrelationContext CorrelationContext
        {
            get => _accessor.CorrelationContext;
            set
            {
                if (value is Correlation correlation)
                {
                    _accessor.CorrelationContext = correlation;
                }
                else
                {
                    _accessor.CorrelationContext = new Correlation { CorrelationId = value?.CorrelationId };
                }
            }
        }
    }
}