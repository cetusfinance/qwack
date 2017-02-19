using System;
using System.Collections.Generic;
using System.Text;

namespace Qwack.Paths
{
    public class FeatureCollection
    {
        private Dictionary<Type,object> _features = new Dictionary<Type, object>();

        public void AddFeature<T>(T featureToAdd)
        {
            _features.Add(typeof(T), featureToAdd);
        }

        public T GetFeature<T>() where T : class
        {
            if (!_features.TryGetValue(typeof(T), out object returnValue))
            {
                return default(T);
            }
            return returnValue as T; 
        }
    }
}
