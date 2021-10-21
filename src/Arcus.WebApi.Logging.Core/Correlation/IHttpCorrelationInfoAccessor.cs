using Arcus.Observability.Correlation;

namespace Arcus.WebApi.Logging.Core.Correlation
{
    /// <summary>
    /// Represents a marker interface for an <see cref="ICorrelationInfoAccessor{TCorrelationInfo}"/> implementation using the HTTP <see cref="CorrelationInfo"/>.
    /// </summary>
    /// <seealso cref="ICorrelationInfoAccessor{TCorrelationInfo}"/>
    public interface IHttpCorrelationInfoAccessor : ICorrelationInfoAccessor<CorrelationInfo>
    {
    }
}
