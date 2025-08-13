using System;
using System.Collections.Generic;
using System.Linq;
using Qwack.Core.Models;

namespace Qwack.Paths
{
    public class FeatureCollection : IFeatureCollection
    {
        private readonly Dictionary<Type, object> _features = new();

        private readonly Dictionary<string, List<object>> _requiredFeatures = new();

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
                    if (!finish.IsComplete)
                    {
                        unfinishedFeatures.Add(finish);
                    }
                }
            }
        }

        private readonly Dictionary<ForwardPriceEstimatorSpec, IForwardPriceEstimate> _forwardPriceEstimators = [];
        public void AddPriceEstimator(ForwardPriceEstimatorSpec spec, IForwardPriceEstimate estimator) => _forwardPriceEstimators[spec] = estimator;
        public IForwardPriceEstimate GetPriceEstimator(ForwardPriceEstimatorSpec spec) => _forwardPriceEstimators[spec];

        public void RegisterRequiredFeature<T>(string category, T feature) where T : class
        {
            if (!_requiredFeatures.TryGetValue(category, out var categoryList))
            {
                categoryList = [];
                _requiredFeatures[category] = categoryList;
            }
            if (categoryList.Any(x=>(x as T).Equals(feature)))
                throw new ArgumentException($"Feature with category '{category}' is already present");
            
            categoryList.Add(feature);
        }

        public void UpdateRequiredFeature<T>(string category, T feature) where T : class
        {
            if (!_requiredFeatures.TryGetValue(category, out var categoryList))
            {
                categoryList = [];
                _requiredFeatures[category] = categoryList;
            }

            var existingFeature = categoryList.FirstOrDefault(x => (x as T).Equals(feature)) ?? throw new ArgumentException($"Feature with category '{category}' not present");

            categoryList.Remove(existingFeature);
            categoryList.Add(feature);
        }

        public bool HasRequiredFeatureRegistration<T>(string category, T feature) where T : class
        {
            if (_requiredFeatures.TryGetValue(category, out var categoryList))
            {
                var existingFeature = categoryList.FirstOrDefault(x => (x as T).Equals(feature));
                return existingFeature != null;
            }
            return false;
        }

        public T GetRequiredFeature<T>(string category, T feature) where T : class
        {
            if (_requiredFeatures.TryGetValue(category, out var categoryList))
            {
                return categoryList.FirstOrDefault(x => (x as T).Equals(feature)) as T;
            }
            return default;
        }

        public T[] GetRequiredFeatures<T>(string category) where T : class
        {
            if (_requiredFeatures.TryGetValue(category, out var categoryList))
            {
                return [.. categoryList.Select(x => x as T)];
            }
            else
            {
                return [];
            }
        }
    }
}
