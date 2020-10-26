using System;
using System.Linq;

namespace Arcus.WebApi.Tests.Unit.Security
{
    /// <summary>
    /// Class with methods to be reused across the security unit tests
    /// </summary>
    public class Util
    {
        /// <summary>
        /// Creates a random string of fixed length
        /// </summary>
        /// <param name="length">Length of the string to return</param>
        /// <returns>A random string with a fixed-length</returns>
        public static string GetRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

            Random random = new Random();

            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
