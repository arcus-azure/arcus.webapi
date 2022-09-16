using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using Bogus;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Arcus.WebApi.Tests.Unit.Logging.Fixture.AzureFunctions
{
    public class TestHttpRequestData : HttpRequestData
    {
        private static readonly Faker BogusGenerator = new Faker();

        public TestHttpRequestData(FunctionContext functionContext) : base(functionContext)
        {
        }

        public TestHttpRequestData(
            Uri url,
            string method,
            Stream body,
            HttpHeadersCollection headers,
            FunctionContext context)
            : base(context)
        {
            Url = url;
            Method = method;
            Body = body;
            Headers = headers;
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

        public static TestHttpRequestData Generate(FunctionContext context)
        {
            return new TestHttpRequestData(
                new Uri(BogusGenerator.Internet.UrlWithPath()),
                BogusGenerator.PickRandom<HttpMethod>().ToString(),
                Stream.Null,
                new HttpHeadersCollection(),
                context);
        }
    }
}