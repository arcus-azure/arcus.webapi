using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Sdk;

namespace Arcus.WebApi.Tests.Integration.Logging.Fixture
{
    /// <summary>
    /// Represents an instance that provides <see cref="HttpAssert"/> instances based on a registered name.
    /// </summary>
    public class HttpAssertProvider
    {
        private readonly Tuple<string, HttpAssert>[] _namedAssertions;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpAssertProvider" /> class.
        /// </summary>
        /// <param name="namedAssertions">The registered series of <see cref="HttpAssert"/> that can be retrieved by name.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="namedAssertions"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="namedAssertions"/> contains <c>null</c> elements or has duplicate names.</exception>
        public HttpAssertProvider(IEnumerable<Tuple<string, HttpAssert>> namedAssertions)
        {
            if (namedAssertions is null)
            {
                throw new ArgumentNullException(nameof(namedAssertions), "Requires a series of named HTTP assertions to setup the HTTP assertion provider");
            }

            if (namedAssertions.Any(item => item is null))
            {
                throw new ArgumentException("Requires a series of named HTTP assertions without any 'null' elements to setup the HTTP assertion provider", nameof(namedAssertions));
            }

            if (namedAssertions.GroupBy(item => item.Item1).All(group => group.Count() != 1))
            {
                throw new ArgumentException("Requires a series of named HTTP assertions with unique names to setup the HTTP assertion provider", nameof(namedAssertions));
            }

            _namedAssertions = namedAssertions.ToArray();
        }

        /// <summary>
        /// Get an <see cref="HttpAssert"/> instance registered under the given <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name under which the <see cref="HttpAssert"/> was registered.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="name"/> is blank.</exception>
        /// <exception cref="SingleException">Thrown when more than one <see cref="HttpAssert"/> was registered under the given <paramref name="name"/>.</exception>
        public HttpAssert GetAssertion(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Requires a non-blank name to retrieve the HTTP assertion", nameof(name));
            }

            return Assert.Single(_namedAssertions, item => item.Item1 == name).Item2;
        }
    }
}