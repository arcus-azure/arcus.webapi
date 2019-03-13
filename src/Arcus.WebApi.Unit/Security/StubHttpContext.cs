using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Primitives;

namespace Arcus.WebApi.Unit.Security
{
    /// <summary>
    /// Stub HTTP context to test request and response calls.
    /// </summary>
    public class StubHttpContext : HttpContext
    {
        private readonly Dictionary<string, StringValues> _requestHeaders;

        /// <summary>
        /// Initializes a new instance of the <see cref="StubHttpContext"/> class.
        /// </summary>
        public StubHttpContext(
            Dictionary<string, StringValues> requestHeaders, 
            IServiceProvider requestServices)
        {
            _requestHeaders = requestHeaders;
            RequestServices = requestServices;
        }

        /// <summary>Aborts the connection underlying this request.</summary>
        public override void Abort()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the collection of HTTP features provided by the server and middleware available on this request.
        /// </summary>
        public override IFeatureCollection Features => throw new NotImplementedException();

        /// <summary>
        /// Gets the <see cref="T:Microsoft.AspNetCore.Http.HttpRequest" /> object for this request.
        /// </summary>
        public override HttpRequest Request => new StubHttpRequest(_requestHeaders);

        /// <summary>
        /// Gets the <see cref="T:Microsoft.AspNetCore.Http.HttpResponse" /> object for this request.
        /// </summary>
        public override HttpResponse Response => throw new NotImplementedException();

        /// <summary>
        /// Gets information about the underlying connection for this request.
        /// </summary>
        public override ConnectionInfo Connection => throw new NotImplementedException();

        /// <summary>
        /// Gets an object that manages the establishment of WebSocket connections for this request.
        /// </summary>
        public override WebSocketManager WebSockets => throw new NotImplementedException();

        /// <summary>
        /// Gets an object that facilitates authentication for this request.
        /// </summary>
        public override AuthenticationManager Authentication => throw new NotImplementedException();

        /// <summary>Gets or sets the the user for this request.</summary>
        public override ClaimsPrincipal User
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        /// <summary>
        /// Gets or sets a key/value collection that can be used to share data within the scope of this request.
        /// </summary>
        public override IDictionary<object, object> Items
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        /// <summary>
        /// Gets or sets the <see cref="T:System.IServiceProvider" /> that provides access to the request's service container.
        /// </summary>
        public sealed override IServiceProvider RequestServices { get; set; }

        /// <summary>
        /// Notifies when the connection underlying this request is aborted and thus request operations should be
        /// cancelled.
        /// </summary>
        public override CancellationToken RequestAborted
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        /// <summary>
        /// Gets or sets a unique identifier to represent this request in trace logs.
        /// </summary>
        public override string TraceIdentifier
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        /// <summary>
        /// Gets or sets the object used to manage user session data for this request.
        /// </summary>
        public override ISession Session
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Stub HTTP request to represent a valid call.
    /// </summary>
    public class StubHttpRequest : HttpRequest
    {
        private Dictionary<string, StringValues> _headers;

        /// <summary>
        /// Initializes a new instance of the <see cref="StubHttpRequest"/> class.
        /// </summary>
        public StubHttpRequest(Dictionary<string, StringValues> headers)
        {
            _headers = headers;
        }

        /// <summary>Reads the request body if it is a form.</summary>
        /// <returns></returns>
        public override async Task<IFormCollection> ReadFormAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the <see cref="P:Microsoft.AspNetCore.Http.HttpRequest.HttpContext" /> this request;
        /// </summary>
        public override HttpContext HttpContext => throw new NotImplementedException();

        /// <summary>Gets or set the HTTP method.</summary>
        /// <returns>The HTTP method.</returns>
        public override string Method
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        /// <summary>Gets or set the HTTP request scheme.</summary>
        /// <returns>The HTTP request scheme.</returns>
        public override string Scheme
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        /// <summary>Returns true if the RequestScheme is https.</summary>
        /// <returns>true if this request is using https; otherwise, false.</returns>
        public override bool IsHttps
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        /// <summary>Gets or set the Host header. May include the port.</summary>
        /// <return>The Host header.</return>
        public override HostString Host
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        /// <summary>Gets or set the RequestPathBase.</summary>
        /// <returns>The RequestPathBase.</returns>
        public override PathString PathBase
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        /// <summary>Gets or set the request path from RequestPath.</summary>
        /// <returns>The request path from RequestPath.</returns>
        public override PathString Path
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        /// <summary>
        /// Gets or set the raw query string used to create the query collection in Request.Query.
        /// </summary>
        /// <returns>The raw query string.</returns>
        public override QueryString QueryString
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the query value collection parsed from Request.QueryString.
        /// </summary>
        /// <returns>The query value collection parsed from Request.QueryString.</returns>
        public override IQueryCollection Query
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        /// <summary>Gets or set the RequestProtocol.</summary>
        /// <returns>The RequestProtocol.</returns>
        public override string Protocol
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        /// <summary>Gets the request headers.</summary>
        /// <returns>The request headers.</returns>
        public override IHeaderDictionary Headers => new HeaderDictionary(_headers);

        /// <summary>Gets the collection of Cookies for this request.</summary>
        /// <returns>The collection of Cookies for this request.</returns>
        public override IRequestCookieCollection Cookies
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        /// <summary>Gets or sets the Content-Length header</summary>
        public override long? ContentLength
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        /// <summary>Gets or sets the Content-Type header.</summary>
        /// <returns>The Content-Type header.</returns>
        public override string ContentType
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        /// <summary>Gets or set the RequestBody Stream.</summary>
        /// <returns>The RequestBody Stream.</returns>
        public override Stream Body
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        /// <summary>Checks the content-type header for form types.</summary>
        public override bool HasFormContentType => throw new NotImplementedException();

        /// <summary>Gets or sets the request body as a form.</summary>
        public override IFormCollection Form
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }
    }
}
