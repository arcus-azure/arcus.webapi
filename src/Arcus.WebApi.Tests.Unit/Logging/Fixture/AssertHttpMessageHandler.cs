using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Arcus.WebApi.Tests.Unit.Logging.Fixture
{
    /// <summary>
    /// Represents an <see cref="HttpMessageHandler"/> that asserts on a send request.
    /// </summary>
    public class AssertHttpMessageHandler : DelegatingHandler
    {
        private readonly HttpStatusCode? _statusCode;
        private readonly Action<HttpRequestMessage> _assertion;

        /// <summary>
        /// Initializes a new instance of the <see cref="AssertHttpMessageHandler" /> class.
        /// </summary>
        public AssertHttpMessageHandler(HttpStatusCode statusCode, Action<HttpRequestMessage> assertion)
        {
            _statusCode = statusCode;
            _assertion = assertion;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AssertHttpMessageHandler" /> class.
        /// </summary>
        public AssertHttpMessageHandler(Action<HttpRequestMessage> assertion)
        {
            _assertion = assertion;
        }

        /// <summary>
        /// Sends an HTTP request to the inner handler to send to the server as an asynchronous operation.
        /// </summary>
        /// <param name="request">The HTTP request message to send to the server.</param>
        /// <param name="cancellationToken">A cancellation token to cancel operation.</param>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="request" /> was <see langword="null" />.</exception>
        /// <returns>The task object representing the asynchronous operation.</returns>
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _assertion(request);

            if (_statusCode is null)
            {
                return base.SendAsync(request, cancellationToken);
            }

            return Task.FromResult(new HttpResponseMessage(_statusCode.Value));
        }
    }
}
