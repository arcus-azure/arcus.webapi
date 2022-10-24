using System;
using System.Diagnostics;
using GuardNet;
using Microsoft.Extensions.Primitives;

// ReSharper disable once CheckNamespace
namespace Microsoft.AspNetCore.Http
{
    /// <summary>
    /// Extensions on the <see cref="IHeaderDictionary"/> to retrieve headers related to HTTP correlation.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class IHeaderDictionaryExtensions
    {
        /// <summary>
        /// Gets the 'traceparent' header value from the HTTP request <paramref name="headers"/>.
        /// </summary>
        /// <param name="headers">The HTTP request headers where the 'traceparent' header is located.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="headers"/> is <c>null</c>.</exception>
        internal static StringValues GetTraceParent(this IHeaderDictionary headers)
        {
            Guard.NotNull(headers, nameof(headers), "Requires a HTTP request headers dictionary instance to retrieve the 'traceparent' header value");
#if NET6_0
            StringValues traceParent = headers.TraceParent;
#else
            StringValues traceParent = headers["traceparent"];
#endif

            if (traceParent == StringValues.Empty)
            {
                return traceParent;
            }

            return traceParent.TruncateString(55);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="headers"></param>
        /// <returns></returns>
        public static (string transactionId, string operationParentId) DeconstructTraceParent(
            this IHeaderDictionary headers)
        {
            string traceParent = GetTraceParent(headers).TruncateString(55);
            string transactionId = ActivityTraceId.CreateFromString(traceParent.AsSpan(3, 32)).ToHexString();
            string operationParentId = ActivitySpanId.CreateFromString(traceParent.AsSpan(36, 16)).ToHexString();

            return (transactionId, operationParentId);
        }

        /// <summary>
        /// Gets the 'tracestate' header value from the HTTP request <paramref name="headers"/>.
        /// </summary>
        /// <param name="headers">The HTTP request headers where the 'tracestate' header is located.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="headers"/> is <c>null</c>.</exception>
        internal static StringValues GetTraceState(this IHeaderDictionary headers)
        {
            Guard.NotNull(headers, nameof(headers), "Requires a HTTP request headers dictionary instance to retrieve the 'tracestate' header value");
#if NET6_0
            return headers.TraceState; 
#else
            return headers["tracestate"];
#endif
        }
    }
}
