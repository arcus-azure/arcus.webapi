using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Arcus.Observability.Correlation;
using Arcus.WebApi.Logging.Core.Correlation;

namespace Arcus.WebApi.Logging.AzureFunctions.Correlation
{
    /// <summary>
    /// Represents an <see cref="IHttpCorrelationInfoAccessor"/> implementation that solely retrieves the correlation information from the <see cref="Activity.Current"/>.
    /// Mostly used for places where the Application Insights is baked in and there is no way to hook in custom Arcus functionality.
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal class ActivityCorrelationInfoAccessor : IHttpCorrelationInfoAccessor, ICorrelationInfoAccessor
    {
        /// <summary>
        /// Gets the current correlation information initialized in this context.
        /// </summary>
        public CorrelationInfo GetCorrelationInfo()
        {
            var activity = Activity.Current;
            if (activity == null)
            {
                return null;
            }

            if (activity.IdFormat == ActivityIdFormat.W3C)
            {
                string operationParentId = DetermineW3CParentId(activity);
                return new CorrelationInfo(
                    activity.SpanId.ToHexString(),
                    activity.TraceId.ToHexString(),
                    operationParentId);
            }

            return new CorrelationInfo(
                activity.Id,
                activity.RootId,
                activity.ParentId);
        }

        private static string DetermineW3CParentId(Activity activity)
        {
            if (activity.ParentSpanId != default)
            {
                return activity.ParentSpanId.ToHexString();
            }
            
            if (!string.IsNullOrEmpty(activity.ParentId))
            {
                // W3C activity with non-W3C parent must keep parentId
                return activity.ParentId;
            }

            return null;
        }

        /// <summary>
        /// Sets the current correlation information for this context.
        /// </summary>
        /// <param name="correlationInfo">The correlation model to set.</param>
        public void SetCorrelationInfo(CorrelationInfo correlationInfo)
        {
            throw new InvalidOperationException(
                "Cannot set new correlation information in Azure Functions in-process model");
        }
    }
}
