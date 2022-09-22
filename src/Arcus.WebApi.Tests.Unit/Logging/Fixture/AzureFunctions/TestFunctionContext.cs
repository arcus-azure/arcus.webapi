using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Arcus.WebApi.Tests.Unit.Logging.Fixture.AzureFunctions
{
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
            return Create(context =>
            {
                var request = new TestHttpRequestData(context);
                configureHttpRequest?.Invoke(request);
                return request;
            }, configureServices);
        }

        public static FunctionContext Create(
            Func<FunctionContext, HttpRequestData> createHttpRequest,
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
                    var request = createHttpRequest(context);

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
}
