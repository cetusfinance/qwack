using Qwack.Core.Models;
using Qwack.Paths.Features;
using System;
using System.Collections.Generic;
using System.Text;

namespace Qwack.Paths
{
    public class FeatureCollection : IFeatureCollection
    {
        private Dictionary<Type, object> _features = new Dictionary<Type, object>();

        public void AddFeature<T>(T featureToAdd) => _features.Add(typeof(T), featureToAdd);

        public T GetFeature<T>() where T : class
        {
            if (!_features.TryGetValue(typeof(T), out var returnValue))
            {
                return default;
            }
            return returnValue as T;
        }

        public void FinishSetup(List<IRequiresFinish> unfinishedFeatures)
        {
            foreach (var feature in _features)
            {
                if (feature.Value is IRequiresFinish finish)
                {
                    finish.Finish(this);
                    if(!finish.IsComplete)
                    {
                        unfinishedFeatures.Add(finish);
                    }
                }
            }
        }
    }
}
