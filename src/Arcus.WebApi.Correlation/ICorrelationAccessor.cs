using Correlate;

namespace Arcus.WebApi.Correlation 
{
    /// <summary>
    /// Provides access to the <see cref="Correlation"/>.
    /// </summary>
    public interface ICorrelationAccessor : ICorrelationContextAccessor
    {
        /// <summary>
        /// Gets the transactional ID of the request.
        /// </summary>
        string CorrelationId { get; }

        /// <summary>
        /// Gets the unique operation ID of the request.
        /// </summary>
        string RequestId { get; }
    }
}