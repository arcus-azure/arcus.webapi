using Microsoft.AspNetCore.Http;

namespace Arcus.WebApi.Unit.Correlation 
{
    /// <summary>
    /// Represents the options available for the <see cref="TraceIdentifierMiddleware"/> component.
    /// </summary>
    public class TraceIdentifierOptions
    {
        /// <summary>
        /// Gets or sets the value to indicate that the <see cref="HttpContext.TraceIdentifier"/> should be removed from the request information.
        /// </summary>
        public bool EnableTraceIdentifier { get; set; }
    }
}