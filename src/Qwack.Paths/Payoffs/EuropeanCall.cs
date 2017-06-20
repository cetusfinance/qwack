using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Linq;
using Qwack.Paths.Features;

namespace Qwack.Paths.Payoffs
{
    public class EuropeanCall : IPathProcess, IFeatureRequiresFinish
    {
        private DateTime _expiry;
        private double _strike;
        private string _assetName;
        private int _assetIndex;
        private int _expiryIndex;
        private List<Vector<double>> _results = new List<Vector<double>>();

        public EuropeanCall(string assetName, double strike, DateTime expiry)
        {
            _expiry = expiry;
            _strike = strike;
            _assetName = assetName;
        }

        public void Finish(FeatureCollection collection)
        {
            var dims = collection.GetFeature<IPathMappingFeature>();
            _assetIndex = dims.GetDimension(_assetName);

            var dates = collection.GetFeature<ITimeStepsFeature>();
            _expiryIndex = dates.GetDateIndex(_expiry);          
        }

        public void Process(PathBlock block)
        {
            for(var path = 0; path < block.NumberOfPaths;path += Vector<double>.Count)
            {
                var steps = block.GetStepsForFactor(path, _assetIndex);
                var finalValues = steps[_expiryIndex] - (new Vector<double>(_strike));
                finalValues = Vector.Max(new Vector<double>(0), finalValues);
                _results.Add(finalValues);
            }
        }

        public void SetupFeatures(FeatureCollection pathProcessFeaturesCollection)
        {
            var dates = pathProcessFeaturesCollection.GetFeature<ITimeStepsFeature>();
            dates.AddDate(_expiry);
        }

        public double AverageResult { get {
                return _results.Select(x =>
                {
                    var vec = new double[Vector<double>.Count];
                    x.CopyTo(vec);
                    return vec.Average();
                }).Average();
            } }
    }
}
