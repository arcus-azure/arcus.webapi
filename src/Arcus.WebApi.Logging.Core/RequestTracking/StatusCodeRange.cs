using System;

namespace Arcus.WebApi.Logging
{
    /// <summary>
    /// Represents a HTTP status code range with a minimum and maximum threshold.
    /// </summary>
    public class StatusCodeRange : IEquatable<StatusCodeRange>
    {
        /// <summary>
        /// <para>Initializes a new instance of the <see cref="StatusCodeRange" /> class with the same HTTP status code for both the minimum and maximum threshold.</para>
        /// <para>Used when only a single HTTP status code is allowed in the range.</para>
        /// </summary>
        /// <param name="minimum">The minimum HTTP status code threshold.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="minimum"/> is less than 100.</exception>
        public StatusCodeRange(int minimum) : this(minimum, minimum)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StatusCodeRange" /> class.
        /// </summary>
        /// <param name="minimum">The minimum HTTP status code threshold.</param>
        /// <param name="maximum">The maximum HTTP status code threshold</param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Thrown when the <paramref name="minimum"/> is less than 100,
        ///     or the <paramref name="maximum"/> is greater than 599,
        ///     or the <paramref name="minimum"/> is greater than the <paramref name="maximum"/>.
        /// </exception>
        public StatusCodeRange(int minimum, int maximum)
        {
            if (minimum < 100)
            {
                throw new ArgumentOutOfRangeException(nameof(minimum), "Requires the minimum HTTP status code threshold to not be less than 100");
            }

            if (maximum > 599)
            {
                throw new ArgumentOutOfRangeException(nameof(maximum), "Requires the maximum HTTP status code threshold to not be greater than 599");
            }

            if (minimum > maximum)
            {
                throw new ArgumentOutOfRangeException(nameof(minimum), "Requires the minimum HTTP status code threshold to be less than the maximum HTTP status code threshold");
            }
            
            Minimum = minimum;
            Maximum = maximum;
        }

        /// <summary>
        /// Gets the minimum HTTP status code threshold.
        /// </summary>
        public int Minimum { get; }

        /// <summary>
        /// Gets the maximum HTTP status code threshold.
        /// </summary>
        public int Maximum { get; }

        /// <summary>
        /// Determines whether or not a given <paramref name="statusCode"/> is within the range of this HTTP status code range (inconclusive).
        /// </summary>
        /// <param name="statusCode">The response HTTP status code..</param>
        /// <returns>
        ///     [true] if the <paramref name="statusCode"/> falls within this HTTP status code range; [false] otherwise.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="statusCode"/> is less than 100 or greater than 599.</exception>
        public bool IsWithinRange(int statusCode)
        {
            if (statusCode < 100)
            {
                throw new ArgumentOutOfRangeException(nameof(statusCode), "Requires the response HTTP status code to not be less than 100");
            }

            if (statusCode > 599)
            {
                throw new ArgumentOutOfRangeException(nameof(statusCode), "Requires the response HTTP status code to not be greater than 599");
            }
            
            return Minimum <= statusCode && statusCode <= Maximum;
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>true if the current object is equal to the <paramref name="other">other</paramref> parameter; otherwise, false.</returns>
        public bool Equals(StatusCodeRange other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Minimum == other.Minimum && Maximum == other.Maximum;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>true if the specified object  is equal to the current object; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj is StatusCodeRange other)
            {
                return Equals(other);
            }

            return false;
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return (Minimum.GetHashCode() * 397) ^ Maximum.GetHashCode();
            }
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            if (Minimum == Maximum)
            {
                return Minimum.ToString();
            }

            return $"{Minimum}-{Maximum}";
        }
    }
}
