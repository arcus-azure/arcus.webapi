using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;

namespace Arcus.WebApi.Tests.Integration.Fixture
{
    /// <summary>
    /// Represents a builder model to create <see cref="HttpResponseMessage"/>s in a more dev-friendly manner.
    /// </summary>
    public class HttpRequestBuilder
    {
        private Func<HttpContent> _createContent;
        private readonly string _path;
        private readonly HttpMethod _method;
        private readonly ICollection<KeyValuePair<string, string>> _headers = new Collection<KeyValuePair<string, string>>();
        private readonly ICollection<KeyValuePair<string, string>> _parameters = new Collection<KeyValuePair<string, string>>();
        
        private HttpRequestBuilder(HttpMethod method, string path)
        {
            _method = method;
            _path = path;
        }
        
        /// <summary>
        /// Creates an <see cref="HttpRequestBuilder"/> instance that represents an HTTP GET request to a given <paramref name="route"/>.
        /// </summary>
        /// <remarks>Only the relative route is required, the base endpoint will be prepended upon the creation of the HTTP request.</remarks>
        /// <param name="route">The relative HTTP route.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="route"/> is blank.</exception>
        public static HttpRequestBuilder Get(string route)
        {
            if (string.IsNullOrWhiteSpace(route))
            {
                throw new ArgumentException("Requires a non-blank HTTP relative route to create a HTTP GET request builder instance", nameof(route));
            }

            return new HttpRequestBuilder(HttpMethod.Get, route);
        }

        /// <summary>
        /// Creates an <see cref="HttpRequestBuilder"/> instance that represents an HTTP POST request to a given <paramref name="route"/>.
        /// </summary>
        /// <remarks>Only the relative route is required, the base endpoint will be prepended upon the creation of the HTTP request.</remarks>
        /// <param name="route">The relative HTTP route.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="route"/> is blank.</exception>
        public static HttpRequestBuilder Post(string route)
        {
            if (string.IsNullOrWhiteSpace(route))
            {
                throw new ArgumentException("Requires a non-blank HTTP relative route to create a HTTP POST request builder instance", nameof(route));
            }

            return new HttpRequestBuilder(HttpMethod.Post, route);
        }
        
        /// <summary>
        /// Adds a header to the HTTP request.
        /// </summary>
        /// <param name="headerName">The name of the header.</param>
        /// <param name="headerValue">The value of the header.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="headerName"/> is blank.</exception>
        public HttpRequestBuilder WithHeader(string headerName, object headerValue)
        {
            if (string.IsNullOrWhiteSpace(headerName))
            {
                throw new ArgumentException("Requires a non-blank header name to add the header to the HTTP request builder instance", nameof(headerName));
            }

            _headers.Add(new KeyValuePair<string, string>(headerName, headerValue?.ToString()));

            return this;
        }

        /// <summary>
        /// Adds a query parameter to the HTTP request.
        /// </summary>
        /// <param name="parameterName">The name of the query parameter.</param>
        /// <param name="parameterValue">The value of the query parameter.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="parameterName"/> is blank.</exception>
        public HttpRequestBuilder WithParameter(string parameterName, object parameterValue)
        {
            if (string.IsNullOrWhiteSpace(parameterName))
            {
                throw new ArgumentException("Requires a non-blank query parameter to add the parameter to the HTTP request builder instance", nameof(parameterName));
            }

            _parameters.Add(new KeyValuePair<string, string>(parameterName, parameterValue.ToString()));

            return this;
        }

        /// <summary>
        /// Adds a JSON text to the HTTP request.
        /// </summary>
        /// <remarks>This is a non-accumulative method, multiple calls will override the request body, not append to it.</remarks>
        /// <param name="text">The JSON request text.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="text"/> is blank.</exception>
        public HttpRequestBuilder WithJsonText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException("Requires a non-blank JSON request text to add the content to the HTTP request builder instance", nameof(text));
            }

            _createContent = () => new StringContent($"\"{text}\"", Encoding.UTF8, "application/json");

            return this;
        }

        /// <summary>
        /// Adds a JSON json to the HTTP request.
        /// </summary>
        /// <remarks>This is a non-accumulative method, multiple calls will override the request body, not append to it.</remarks>
        /// <param name="json">The JSON request body.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="json"/> is blank.</exception>
        public HttpRequestBuilder WithJsonBody(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("Requires a non-blank JSON request body to add the content to the HTTP request builder instance", nameof(json));
            }

            _createContent = () => new StringContent(json, Encoding.UTF8, "application/json");

            return this;
        } 

        /// <summary>
        /// Adds a plain text body to the HTTP request.
        /// </summary>
        /// <remarks>This is a non-accumulative method, multiple calls will override the request body, not append to it.</remarks>
        /// <param name="text">The plain text request body.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="text"/> is blank.</exception>
        public HttpRequestBuilder WithTextBody(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException("Requires a non-blank text input for the request body", nameof(text));
            }

            _createContent = () => new StringContent(text, Encoding.UTF8, "text/plain");

            return this;
        }
        
        /// <summary>
        /// Builds the actual <see cref="HttpRequestMessage"/> with the previously provided configurations.
        /// </summary>
        /// <param name="baseRoute">The base route of the HTTP request.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="baseRoute"/> is blank.</exception>
        /// <exception cref="UriFormatException">Thrown when the <paramref name="baseRoute"/> is not in the correct HTTP format.</exception>
        internal HttpRequestMessage Build(string baseRoute)
        {
            if (string.IsNullOrWhiteSpace(baseRoute))
            {
                throw new ArgumentException("Requires a non-blank base HTTP endpoint to create a HTTP request message from the HTTP request builder instance", nameof(baseRoute));
            }


            string parameters = "";
            
            if (_parameters.Count > 0)
            {
                parameters = "?" + String.Join("&", _parameters.Select(p => $"{p.Key}={p.Value}")); 
            }

            string path = _path;
            
            if (path.StartsWith('/'))
            {
                path = path.TrimStart('/');
            }

            var request = new HttpRequestMessage(_method, baseRoute + path + parameters);

            foreach (KeyValuePair<string, string> header in _headers)
            {
                request.Headers.Add(header.Key, header.Value);
            }

            if (_createContent != null)
            {
                request.Content = _createContent();
            }

            return request;
        }
    }
}