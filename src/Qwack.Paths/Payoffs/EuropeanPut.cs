using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Linq;
using Qwack.Paths.Features;

namespace Qwack.Paths.Payoffs
{
    public class EuropeanPut : IPathProcess, IRequiresFinish
    {
        private DateTime _expiry;
        private double _strike;
        private string _assetName;
        private int _assetIndex;
        private int _expiryIndex;
        private bool _isComplete;
        private List<Vector<double>> _results = new List<Vector<double>>();

        public EuropeanPut(string assetName, double strike, DateTime expiry)
        {
            _expiry = expiry;
            _strike = strike;
            _assetName = assetName;
        }

        public bool IsComplete => _isComplete;

        public void Finish(FeatureCollection collection)
        {
            var dims = collection.GetFeature<IPathMappingFeature>();
            _assetIndex = dims.GetDimension(_assetName);

            var dates = collection.GetFeature<ITimeStepsFeature>();
            _expiryIndex = dates.GetDateIndex(_expiry);
            _isComplete = true;
        }

        public void Process(PathBlock block)
        {
            for (var path = 0; path < block.NumberOfPaths; path += Vector<double>.Count)
            {
                var steps = block.GetStepsForFactor(path, _assetIndex);
                var finalValues = (new Vector<double>(_strike)) - steps[_expiryIndex];
                finalValues = Vector.Max(new Vector<double>(0), finalValues);
                _results.Add(finalValues);
            }
        }

        public void SetupFeatures(FeatureCollection pathProcessFeaturesCollection)
        {
            var dates = pathProcessFeaturesCollection.GetFeature<ITimeStepsFeature>();
            dates.AddDate(_expiry);
        }

        public double AverageResult => _results.Select(x =>
                {
                    var vec = new double[Vector<double>.Count];
                    x.CopyTo(vec);
                    return vec.Average();
                }).Average();
    }
}
