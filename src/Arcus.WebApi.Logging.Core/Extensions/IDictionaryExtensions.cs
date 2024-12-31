using System.Linq;

// ReSharper disable once CheckNamespace
namespace System.Collections.Generic
{
    /// <summary>
    /// Extra extensions on the <see cref="IDictionary{TKey,TValue}"/> type.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class IDictionaryExtensions
    {
        /// <summary>
        /// Filters a dictionary of key/value pairs based on a predicate.
        /// </summary>
        /// <typeparam name="TKey">The type of the unique key in the dictionary.</typeparam>
        /// <typeparam name="TValue">The type of the value in the dictionary.</typeparam>
        /// <param name="dictionary">The dictionary to filter.</param>
        /// <param name="predicate">The function to test each key/value pair.</param>
        /// <returns>
        ///     A dictionary where each key/value pair matches the given <paramref name="predicate"/>.
        /// </returns>
        public static IDictionary<TKey, TValue> Where<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, Func<KeyValuePair<TKey, TValue>, bool> predicate)
        {
            if (dictionary is null)
            {
                throw new ArgumentNullException(paramName: nameof(dictionary));
            }
            if (predicate is null)
            {
                throw new ArgumentNullException(paramName: nameof(predicate));
            }

            return Enumerable.Where(dictionary, predicate)
                             .ToDictionary(item => item.Key, item => item.Value);
        }
    }
}
