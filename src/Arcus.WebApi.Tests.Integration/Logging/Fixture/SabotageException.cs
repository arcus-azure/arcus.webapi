using System;

namespace Arcus.WebApi.Tests.Integration.Logging.Fixture
{
    /// <summary>
    /// Represents an integration test exception to simulate exceptions.
    /// </summary>
    [Serializable]
    public class SabotageException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SabotageException" /> class.
        /// </summary>
        public SabotageException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SabotageException" /> class.
        /// </summary>
        /// <param name="message">The message to describe the exception.</param>
        public SabotageException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SabotageException" /> class.
        /// </summary>
        /// <param name="message">The message to describe the exception.</param>
        /// <param name="innerException">The exception that was the cause of the current exception.</param>
        public SabotageException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
