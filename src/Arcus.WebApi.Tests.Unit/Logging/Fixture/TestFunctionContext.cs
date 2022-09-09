using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Claims;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Arcus.WebApi.Tests.Unit.Logging.Fixture
{
    public class TestHttpResponseData : HttpResponseData
    {
        public TestHttpResponseData(FunctionContext functionContext) : base(functionContext)
        {
        }

        public override HttpStatusCode StatusCode { get; set; }
        public override HttpHeadersCollection Headers { get; set; } = new HttpHeadersCollection();
        public override Stream Body { get; set; } = new MemoryStream();
        public override HttpCookies Cookies { get; }
    }

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

    public class TestFunctionContext : FunctionContext, IDisposable
    {
        public TestFunctionContext(FunctionDefinition functionDefinition, IServiceProvider serviceProvider)
        {
            FunctionDefinition = functionDefinition;
            Features = new TestInvocationFeatures();
            InstanceServices = serviceProvider;
        }

        public bool IsDisposed { get; private set; }
        public override IServiceProvider InstanceServices { get; set; }
        public override FunctionDefinition FunctionDefinition { get; }
        public override IDictionary<object, object> Items { get; set; }
        public override IInvocationFeatures Features { get; }
        public override string InvocationId { get; }
        public override string FunctionId { get; }
        public override TraceContext TraceContext { get; }
        public override BindingContext BindingContext { get; }
        public override RetryContext RetryContext { get; }

        public static FunctionContext Create(
            Action<HttpRequestData> configureHttpRequest = null,
            Action<IServiceCollection> configureServices = null)
        {
            var services = new ServiceCollection();
            configureServices?.Invoke(services);
            services.AddLogging();
            services.AddSingleton<FunctionContext>(provider =>
            {
                return new TestFunctionContext(
                    new TestFunctionDefinition(new Dictionary<string, BindingMetadata>
                    {
                        { "req", new TestBindingMetadata("req", "httpTrigger", BindingDirection.In) }
                    }), 
                    provider);
            });

            Type conversionResultIBindingCacheType = 
                GetWorkerCoreType("IBindingCache`1")
                    .MakeGenericType(typeof(ConversionResult));
            
            services.AddSingleton(conversionResultIBindingCacheType,
                provider =>
                {
                    Type conversionResultBindingCacheType = 
                        GetWorkerCoreType("DefaultBindingCache`1")
                            .MakeGenericType(typeof(ConversionResult));
                    object defaultBindingCache = CreateInstance(conversionResultBindingCacheType);

                    var context = provider.GetRequiredService<FunctionContext>();
                    var request = new TestHttpRequestData(context);
                    configureHttpRequest?.Invoke(request);

                    ConversionResult result = ConversionResult.Success(request);
                    InvokeMethod(defaultBindingCache, "TryAdd", "req", result);

                    return defaultBindingCache;
                });

            IServiceProvider provider = services.BuildServiceProvider();
            var context = provider.GetRequiredService<FunctionContext>();

            object invocationRequest = CreateInstance(GetWorkerGrpcType("Messages.InvocationRequest"));
            object outputBindingInfoProvider = CreateInstance(GetWorkerCoreType("OutputBindings.DefaultOutputBindingsInfoProvider"));

            Type bindingFeatureType = GetWorkerGrpcType("Features.GrpcFunctionBindingsFeature");
            object bindingFeature = CreateInstance(bindingFeatureType, context, invocationRequest, outputBindingInfoProvider);
            context.Features.Set(bindingFeature);

            return context;
        }

        private static void InvokeMethod(object instance, string methodName, params object[] parameters)
        {
            MethodInfo method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
            method.Invoke(instance, BindingFlags.Public | BindingFlags.Instance, binder: null, parameters: parameters, culture: null);
        }

        private static object CreateInstance(Type type, params object[] args)
        {
            return Activator.CreateInstance(type, BindingFlags.CreateInstance, binder: null, args: args, culture: null);
        }

        private static Type GetWorkerCoreType(string partialTypeName)
        {
            return Type.GetType($"Microsoft.Azure.Functions.Worker.{partialTypeName},Microsoft.Azure.Functions.Worker.Core", throwOnError: true);
        }

        private static Type GetWorkerGrpcType(string partialTypeName)
        {
            return Type.GetType($"Microsoft.Azure.Functions.Worker.Grpc.{partialTypeName},Microsoft.Azure.Functions.Worker.Grpc", throwOnError: true);
        }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }

    public class TestInvocationFeatures : IInvocationFeatures
    {
        private readonly Dictionary<Type, object> _features = new Dictionary<Type, object>();

        public object Get(Type type)
        {
            KeyValuePair<Type, object> item = _features.FirstOrDefault(feature =>
            {
                bool implementsRequestedInterface = feature.Value.GetType().GetInterfaces().Any(i => i == type);
                return feature.Key == type || implementsRequestedInterface;
            });

            return item.Value;
        }

        public T Get<T>()
        {
            return (T) Get(typeof(T));
        }

        public IEnumerator<KeyValuePair<Type, object>> GetEnumerator() => _features.GetEnumerator();

        public void Set<T>(T instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            _features[typeof(T)] = instance;
        }

        IEnumerator IEnumerable.GetEnumerator() => _features.GetEnumerator();
    }

    public class TestFunctionDefinition : FunctionDefinition
    {
        public TestFunctionDefinition(IDictionary<string, BindingMetadata> inputBindings)
        {
            InputBindings = inputBindings.ToImmutableDictionary();
        }

        public override ImmutableArray<FunctionParameter> Parameters { get; }
        public override string PathToAssembly { get; }
        public override string EntryPoint { get; }
        public override string Id { get; }
        public override string Name { get; }
        public override IImmutableDictionary<string, BindingMetadata> InputBindings { get; }
        public override IImmutableDictionary<string, BindingMetadata> OutputBindings { get; }
    }

    public class TestBindingMetadata : BindingMetadata
    {
        public TestBindingMetadata(string name, string type, BindingDirection direction)
        {
            Name = name;
            Type = type;
            Direction = direction;
        }

        public override string Name { get; }
        public override string Type { get; }
        public override BindingDirection Direction { get; }
    }
}
