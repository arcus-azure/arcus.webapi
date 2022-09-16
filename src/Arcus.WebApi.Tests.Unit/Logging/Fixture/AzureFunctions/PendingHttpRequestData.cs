using System;
using System.Collections.Generic;
using System.IO;
using Bogus;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Arcus.WebApi.Tests.Unit.Logging.Fixture.AzureFunctions
{
    public class PendingHttpRequestData
    {
        private static readonly Faker BogusGenerator = new Faker();

        public string Method { get; set; }
        public Uri Url { get; set; }
        public HttpHeadersCollection Headers { get; set; }
        public Stream Body { get; set; }

        public static PendingHttpRequestData Generate(
            string url = null,
            string headerName = null,
            string headerValue = null,
            Stream body = null)
        {
            var method = BogusGenerator.PickRandom<HttpMethod>().ToString();
            var header = new KeyValuePair<string, string>(headerName ?? BogusGenerator.Lorem.Word(), headerValue ?? BogusGenerator.Lorem.Word());

            return new PendingHttpRequestData
            {
                Url = new Uri(url ?? BogusGenerator.Internet.UrlWithPath()),
                Body = body ?? Stream.Null,
                Headers = new HttpHeadersCollection(new[] { header }),
                Method = method
            };
        }

        public HttpRequestData Activate(FunctionContext context)
        {
            return new TestHttpRequestData(Url, Method, Body, Headers, context);
        }
    }
}