using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Arcus.Testing.Logging;
using Arcus.WebApi.Tests.Integration.Controllers;
using Bogus;
using GuardNet;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;

namespace Arcus.WebApi.Tests.Integration.Fixture
{
    public class ServerOptions
    {
        private readonly Faker _bogusGenerator = new Faker();
        private readonly ICollection<Action<IServiceCollection>> _configureServices = new Collection<Action<IServiceCollection>>();
        private readonly ICollection<Action<IApplicationBuilder>> _preconfigures = new Collection<Action<IApplicationBuilder>>();
        private readonly ICollection<Action<IApplicationBuilder>> _configures = new Collection<Action<IApplicationBuilder>>();
        private readonly ICollection<Action<IHostBuilder>> _hostingConfigures = new Collection<Action<IHostBuilder>>();
        private readonly ICollection<Action<IConfigurationBuilder>> _appConfigures = new Collection<Action<IConfigurationBuilder>>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerOptions" /> class.
        /// </summary>
        public ServerOptions()
        {
            Url = $"http://localhost:{_bogusGenerator.Random.Int(1000, 5999)}/";
        }
        
        internal string Url { get; }
        
        /// <summary>
        /// <para>Adds a function to configure the dependency services on the test API server.</para>
        /// <para>This corresponds with the <see cref="IHostBuilder.ConfigureServices"/> call.</para>
        /// </summary>
        /// <param name="configureServices">The function to configure the dependency services.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="configureServices"/> is <c>null</c>.</exception>
        public ServerOptions ConfigureServices(Action<IServiceCollection> configureServices)
        {
            Guard.NotNull(configureServices, nameof(configureServices), "Requires a function to configure the dependency services on the test API server");
            _configureServices.Add(configureServices);

            return this;
        }

        public ServerOptions PreConfigure(Action<IApplicationBuilder> configure)
        {
            Guard.NotNull(configure, nameof(configure), "Requires a function to configure the application on the test API server");
            _preconfigures.Add(configure);

            return this;
        }
        
        public ServerOptions Configure(Action<IApplicationBuilder> configure)
        {
            Guard.NotNull(configure, nameof(configure), "Requires a function to configure the application on the test API server");
            _configures.Add(configure);

            return this;
        }

        public ServerOptions ConfigureHost(Action<IHostBuilder> configure)
        {
            Guard.NotNull(configure, nameof(configure), "Requires a function to configure the hosting configuration of the test API server");
            _hostingConfigures.Add(configure);

            return this;
        }

        public ServerOptions ConfigureAppConfiguration(Action<IConfigurationBuilder> configure)
        {
            Guard.NotNull(configure, nameof(configure), "Requires a function to configure the application configuration of the test API server");
            _appConfigures.Add(configure);

            return this;
        }
        
        internal void ApplyOptions(IHostBuilder builder)
        {
            foreach (Action<IHostBuilder> hostConfigure in _hostingConfigures)
            {
                hostConfigure(builder);
            }

            builder.ConfigureAppConfiguration(config =>
            {
                foreach (Action<IConfigurationBuilder> appConfigure in _appConfigures)
                {
                    appConfigure(config);
                }
            });
            
            builder.ConfigureServices(services =>
            {
                services.AddRouting()
                        .AddControllers()
                        .AddApplicationPart(typeof(TestApiServer).Assembly);
                
                foreach (Action<IServiceCollection> configureService in _configureServices)
                {
                    configureService(services);
                }
            });

            builder.ConfigureWebHostDefaults(webHost =>
            {
                webHost.ConfigureKestrel(options => { })
                       .UseUrls(Url)
                       .Configure(app =>
                       {
                           foreach (Action<IApplicationBuilder> preconfigure in _preconfigures)
                           {
                               preconfigure(app);
                           }
                           
                           app.UseRouting();

                           foreach (Action<IApplicationBuilder> configure in _configures)
                           {
                               configure(app);
                           }

                           app.UseEndpoints(endpoints => endpoints.MapControllers());
                       });
            });
        }
    }

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
        
        public static HttpRequestBuilder Get(string path)
        {
            return new HttpRequestBuilder(HttpMethod.Get, path);
        }

        public static HttpRequestBuilder Post(string path)
        {
            return new HttpRequestBuilder(HttpMethod.Post, path);
        }
        
        public HttpRequestBuilder WithHeader(string headerName, object headerValue)
        {
            _headers.Add(new KeyValuePair<string, string>(headerName, headerValue.ToString()));
            return this;
        }

        public HttpRequestBuilder WithParameter(string parameterName, object parameterValue)
        {
            _parameters.Add(new KeyValuePair<string, string>(parameterName, parameterValue.ToString()));
            return this;
        }

        public HttpRequestBuilder WithJsonBody(string json)
        {
            _createContent = () => new StringContent($"\"{json}\"", Encoding.UTF8, "application/json");
            return this;
        }

        internal HttpRequestMessage Build(string baseRoute)
        {
            string parameters = "";
            if (_parameters.Count > 0)
            {
                parameters = "?" + String.Join("&", _parameters.Select(p => $"{p.Key}={p.Value}")); 
            }

            string path = _path;
            if (path.StartsWith("/"))
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
    
    public class TestApiServer : IAsyncDisposable
    {
        private readonly IHost _host;
        private readonly ServerOptions _options;
        private readonly ILogger _logger;

        private static readonly HttpClient HttpClient = new HttpClient();

        private TestApiServer(IHost host, ServerOptions options, ILogger logger)
        {
            Guard.NotNull(host, nameof(host), "Requires a 'IHost' instance to start/stop the test API server");
            _host = host;
            _options = options;
            _logger = logger;
        }
        
        public static async Task<TestApiServer> StartNewAsync(ServerOptions options, ILogger logger)
        {
            IHostBuilder builder = Host.CreateDefaultBuilder();
            options.ApplyOptions(builder);
            options.ConfigureServices(services =>
                services.AddLogging(logging => logging.AddProvider(new CustomLoggerProvider(logger))));

            IHost host = builder.Build();
            var server = new TestApiServer(host, options, logger);
            await host.StartAsync();

            return server;
        }

        public async Task<HttpResponseMessage> SendAsync(HttpRequestBuilder builder)
        {
            HttpRequestMessage request = builder.Build(_options.Url);
            try
            {
                HttpResponseMessage response = await HttpClient.SendAsync(request);
                return response;
            }
            catch (Exception exception)
            {
                _logger.LogCritical(exception, "Cannot connect to HTTP endpoint {Method} '{Uri}'", request.Method, request.RequestUri);
                throw;
            }
        }
        
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            await _host.StopAsync();
            _host.Dispose();
        }
    }
}
