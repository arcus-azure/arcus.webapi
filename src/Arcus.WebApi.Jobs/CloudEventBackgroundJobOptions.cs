namespace Arcus.WebApi.Jobs
{
    /// <summary>
    /// Represents the options to configure the <see cref="CloudEventBackgroundJob"/>.
    /// </summary>
    public class CloudEventBackgroundJobOptions
    {
        /// <summary>
        /// Gets or sets the job ID to distinguish background job instances in a multi-deployment.
        /// </summary>
        public string JobId { get; set; }
    }
}
