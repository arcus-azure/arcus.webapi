using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Primitives;

// ReSharper disable once CheckNamespace
namespace System
{
    /// <summary>
    /// Extensions on the <c>string</c> type, related to handling HTTP request header values.
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal static class StringExtensions
    {
        /// <summary>
        /// Truncate the <paramref name="input"/> to a <paramref name="maxLength"/>.
        /// </summary>
        /// <param name="input">The string input to truncate.</param>
        /// <param name="maxLength">The maximum length the <paramref name="input"/> should have, all beyond should be truncated.</param>
        internal static string TruncateString(this StringValues input, int maxLength)
        {
            return TruncateString((string) input, maxLength);
        }

        /// <summary>
        /// Truncate the <paramref name="input"/> to a <paramref name="maxLength"/>.
        /// </summary>
        /// <param name="input">The string input to truncate.</param>
        /// <param name="maxLength">The maximum length the <paramref name="input"/> should have, all beyond should be truncated.</param>
        internal static string TruncateString(this string input, int maxLength)
        {
            if (input != null && input.Length > maxLength)
            {
                input = input.Substring(0, maxLength);
            }

            return input;
        }
    }
}
