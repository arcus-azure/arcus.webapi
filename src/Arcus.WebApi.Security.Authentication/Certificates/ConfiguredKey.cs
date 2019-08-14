using GuardNet;

namespace Arcus.WebApi.Security.Authentication.Certificates
{
    /// <summary>
    /// Represents the configured key on the authentication mechanisms.
    /// </summary>
    internal class ConfiguredKey
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConfiguredKey"/> class.
        /// </summary>
        /// <param name="value">The key value.</param>
        /// <exception cref="System.ArgumentException">When the <paramref name="value"/> is <c>null</c> or blank.</exception>
        internal ConfiguredKey(string value)
        {
            Guard.NotNullOrWhitespace(value, nameof(value), "Configured key value cannot be blank");

            Value = value;
        }

        /// <summary>
        /// Gets the <c>string</c> value of the configured key.
        /// </summary>
        internal string Value { get; }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return Value;
        }
    }
}
