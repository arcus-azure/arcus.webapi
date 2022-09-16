using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.Functions.Worker.Http;

namespace Arcus.WebApi.Logging.AzureFunctions
{
    /// <summary>
    /// Represents an <see cref="HttpRequestData"/> instance with a buffered HTTP request body.
    /// </summary>
    internal class BufferedHttpRequestData : HttpRequestData
    {
        private readonly HttpRequestData _request;

        /// <summary>
        /// Initializes a new instance of the <see cref="BufferedHttpRequestData" /> class.
        /// </summary>
        public BufferedHttpRequestData(HttpRequestData request)
            : base(request.FunctionContext)
        {
            _request = request;

            Url = request.Url;
            Method = request.Method;
            Headers = request.Headers;
            Identities = request.Identities;
            Cookies = request.Cookies;

            var defaultEnableRewindMemoryThreshold = 30720;
            Body = new FileBufferingReadStream(request.Body, defaultEnableRewindMemoryThreshold, bufferLimit: null, Path.GetTempPath);
        }

        /// <summary>
        /// Gets the <see cref="T:System.Uri" /> for this request.
        /// </summary>
        public override Uri Url { get; }

        /// <summary>Gets the HTTP method for this request.</summary>
        public override string Method { get; }

        /// <summary>
        /// Gets a <see cref="T:Microsoft.Azure.Functions.Worker.Http.HttpHeadersCollection" /> containing the request headers.
        /// </summary>
        public override HttpHeadersCollection Headers { get; }

        /// <summary>
        /// A <see cref="T:System.IO.Stream" /> containing the HTTP body data.
        /// </summary>
        public override Stream Body { get; }

        /// <summary>
        /// Gets an <see cref="T:System.Collections.Generic.IEnumerable`1" /> containing the request identities.
        /// </summary>
        public override IEnumerable<ClaimsIdentity> Identities { get; }

        /// <summary>
        /// Gets an <see cref="T:System.Collections.Generic.IReadOnlyCollection`1" /> containing the request cookies.
        /// </summary>
        public override IReadOnlyCollection<IHttpCookie> Cookies { get; }

        /// <summary>Creates a response for this request.</summary>
        /// <returns>The response instance.</returns>
        public override HttpResponseData CreateResponse()
        {
            return _request.CreateResponse();
        }
    }
}