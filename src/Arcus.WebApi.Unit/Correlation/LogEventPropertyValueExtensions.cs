using Serilog.Events;

namespace Arcus.WebApi.Unit.Correlation
{
    /// <summary>
    /// Extensions on the <see cref="LogEventPropertyValue"/>
    /// </summary>
    public static class LogEventPropertyValueExtensions
    {
        /// <summary>
        /// Gets the value of the logged property value as a string.
        /// </summary>
        /// <param name="property">The property to get the value.</param>
        public static string ToStringValue(this LogEventPropertyValue property)
        {
            return property.ToString().Trim('\"');
        }
    }
}
