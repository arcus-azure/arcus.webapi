namespace Arcus.WebApi.Security
{
    /// <summary>
    /// Represents a dev-friendly way to indicate in the security logging whether or not a security function ran successfully or ran into a failure.
    /// </summary>
    internal enum SecurityResult
    {
        /// <summary>
        /// Sets the security function result to success.
        /// </summary>
        Success = 0,

        /// <summary>
        /// Sets the security function result to failure.
        /// </summary>
        Failure = 1
    }
}
