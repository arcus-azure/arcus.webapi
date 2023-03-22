using System;
using GuardNet;
using Microsoft.AspNetCore.Http;

namespace Arcus.WebApi.Tests.Integration.Logging.Fixture
{
    /// <summary>
    /// Represents an assertion function on the <see cref="HttpContext"/>.
    /// </summary>
    public class HttpAssert
    {
        private readonly Action<HttpContext> _assertion;

        private HttpAssert(Action<HttpContext> assertion)
        {
            Guard.NotNull(assertion, nameof(assertion), "Requires an assertion function to verify a HTTP context");
            _assertion = assertion;
        }

        /// <summary>
        /// Creates a <see cref="HttpAssert"/> model that asserts on a given <see cref="HttpContext"/>.
        /// </summary>
        /// <param name="assertion">The assertion to run against the <see cref="HttpContext"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="assertion"/> is <c>null</c>.</exception>
        public static HttpAssert Create(Action<HttpContext> assertion)
        {
            Guard.NotNull(assertion, nameof(assertion), "Requires an assertion function to verify a HTTP context");
            return new HttpAssert(assertion);
        }

        /// <summary>
        /// Asserts on a HTTP <paramref name="context"/>.
        /// </summary>
        /// <param name="context">The currently available HTTP context that needs to be asserted.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="context"/> is <c>null</c>.</exception>
        public void Assert(HttpContext context)
        {
            Guard.NotNull(context, nameof(context), "Requires a HTTP context to run an assertion function on it");
            _assertion(context);
        }
    }
}
