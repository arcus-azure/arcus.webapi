using Arcus.Observability.Correlation;
using Arcus.WebApi.Logging.Core.Correlation;

namespace Arcus.WebApi.Logging.AzureFunctions.Correlation
{
    /// <summary>
    /// 
    /// </summary>
    public class AzureFunctionsHttpCorrelationInfoAccessor : IHttpCorrelationInfoAccessor
    {
        private readonly IFunctionContextAccessor _contextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureFunctionsHttpCorrelationInfoAccessor" /> class.
        /// </summary>
        public AzureFunctionsHttpCorrelationInfoAccessor(IFunctionContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor;
        }

        /// <summary>
        /// Gets the current correlation information initialized in this context.
        /// </summary>
        public CorrelationInfo GetCorrelationInfo()
        {
            return _contextAccessor.FunctionContext.Features.Get<CorrelationInfo>();
        }

        /// <summary>
        /// Sets the current correlation information for this context.
        /// </summary>
        /// <param name="correlationInfo">The correlation model to set.</param>
        public void SetCorrelationInfo(CorrelationInfo correlationInfo)
        {
            _contextAccessor.FunctionContext.Features.Set(correlationInfo);
        }
    }
}
