using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Qwack.Core.Models;
using Qwack.Math;
using Qwack.Paths.Features;

namespace Qwack.Paths.Payoffs
{
    public class EuropeanCall : IPathProcess, IRequiresFinish
    {
        private readonly DateTime _expiry;
        private readonly double _strike;
        private readonly string _assetName;
        private int _assetIndex;
        private int _expiryIndex;
        private readonly List<Vector<double>> _results = new();
        private bool _isComplete;

        public EuropeanCall(string assetName, double strike, DateTime expiry)
        {
            _expiry = expiry;
            _strike = strike;
            _assetName = assetName;
        }

        public bool IsComplete => _isComplete;

        public void Finish(IFeatureCollection collection)
        {
            var dims = collection.GetFeature<IPathMappingFeature>();
            _assetIndex = dims.GetDimension(_assetName);

            var dates = collection.GetFeature<ITimeStepsFeature>();
            _expiryIndex = dates.GetDateIndex(_expiry);
            _isComplete = true;
        }

        public void Process(IPathBlock block)
        {
            var numPaths = block.NumberOfPaths;
            var strike = new Vector<double>(_strike);
            for (var path = 0; path < numPaths; path += Vector<double>.Count)
            {
                var steps = block.GetStepsForFactor(path, _assetIndex);
                var finalValues = steps[_expiryIndex] - strike;
                finalValues = Vector.Max(new Vector<double>(0), finalValues);
                _results.Add(finalValues);
            }
        }

        public void SetupFeatures(IFeatureCollection pathProcessFeaturesCollection)
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

        public double ResultStdError => _results.SelectMany(x =>
        {
            var vec = new double[Vector<double>.Count];
            x.CopyTo(vec);
            return vec;
        }).StdDev();
    }
}
