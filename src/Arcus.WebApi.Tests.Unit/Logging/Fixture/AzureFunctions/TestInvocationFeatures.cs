using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Functions.Worker;

namespace Arcus.WebApi.Tests.Unit.Logging.Fixture.AzureFunctions
{
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
            return (T)Get(typeof(T));
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
}