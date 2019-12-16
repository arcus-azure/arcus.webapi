namespace Arcus.WebApi.Correlation
{
    /// <summary>
    /// Correlation options specific to the transaction ID.
    /// </summary>
    public class CorrelationOptionsTransaction
    {
        /// <summary>
        /// Gets or sets whether to include the transaction ID in the response.
        /// </summary>
        /// <remarks>
        ///     A common use case is to disable tracing info in edge services, so that such details are not exposed to the outside world.
        /// </remarks>
        public bool IncludeInResponse { get; set; } = true;
    }
}
