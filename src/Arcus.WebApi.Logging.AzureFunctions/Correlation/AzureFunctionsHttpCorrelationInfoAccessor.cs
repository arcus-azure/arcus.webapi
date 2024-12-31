using System;
using Arcus.Observability.Correlation;
using Arcus.WebApi.Logging.Core.Correlation;
using Microsoft.Azure.Functions.Worker;

namespace Arcus.WebApi.Logging.AzureFunctions.Correlation
{
    /// <summary>
    /// Represents an <see cref="IHttpCorrelationInfoAccessor"/> implementation that gets and sets the <see cref="CorrelationInfo"/> in an Azure Functions environment
    /// with the <see cref="FunctionContext"/> model.
    /// </summary>
    public class AzureFunctionsHttpCorrelationInfoAccessor : IHttpCorrelationInfoAccessor
    {
        private readonly IFunctionContextAccessor _contextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureFunctionsHttpCorrelationInfoAccessor" /> class.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="contextAccessor"/> is <c>null</c>.</exception>
        public AzureFunctionsHttpCorrelationInfoAccessor(IFunctionContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor),  "Requires a function context accessor instance to get/set the correlation information in the function context");
        }

        /// <summary>
        /// Gets the current correlation information initialized in this context.
        /// </summary>
        public CorrelationInfo GetCorrelationInfo()
        {
            return _contextAccessor.FunctionContext?.Features?.Get<CorrelationInfo>();
        }

        /// <summary>
        /// Sets the current correlation information for this context.
        /// </summary>
        /// <param name="correlationInfo">The correlation model to set.</param>
        public void SetCorrelationInfo(CorrelationInfo correlationInfo)
        {
            _contextAccessor.FunctionContext?.Features?.Set(correlationInfo);
        }
    }
}
