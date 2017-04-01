using Qwack.Paths.Features;
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

        public void FinishSetup()
        {
            foreach(var feature in _features)
            {
                if (feature.Value is IFeatureRequiresFinish finish)
                {
                    finish.Finish(this);
                }
            }
        }
    }
}
