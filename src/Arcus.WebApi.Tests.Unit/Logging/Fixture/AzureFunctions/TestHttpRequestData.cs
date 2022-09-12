using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Arcus.WebApi.Tests.Unit.Logging.Fixture.AzureFunctions
{
    public class TestHttpRequestData : HttpRequestData
    {
        public TestHttpRequestData(FunctionContext functionContext) : base(functionContext)
        {
        }

        public override HttpResponseData CreateResponse()
        {
            return new TestHttpResponseData(FunctionContext);
        }

        public override Stream Body { get; }
        public override HttpHeadersCollection Headers { get; } = new HttpHeadersCollection();
        public override IReadOnlyCollection<IHttpCookie> Cookies { get; }
        public override Uri Url { get; }
        public override IEnumerable<ClaimsIdentity> Identities { get; }
        public override string Method { get; }
    }
}