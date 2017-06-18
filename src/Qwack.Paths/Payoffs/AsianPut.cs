using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Qwack.Paths.Features;

namespace Qwack.Paths.Payoffs
{
    public class AsianPut : IPathProcess, IFeatureRequiresFinish
    {
        private List<DateTime> _asianDates;
        private double _strike;
        private string _assetName;
        private int _assetIndex;
        private int[] _dateIndexes;
        private List<Vector<double>> _results = new List<Vector<double>>();

        public AsianPut(string assetName, double strike, List<DateTime> asianingDates)
        {
            _asianDates = asianingDates;
            _assetName = assetName;
            _strike = strike;
        }

        public void Finish(FeatureCollection collection)
        {
            var dims = collection.GetFeature<IPathMappingFeature>();
            _assetIndex = dims.GetDimension(_assetName);

            var dates = collection.GetFeature<ITimeStepsFeature>();
            _dateIndexes = new int[_asianDates.Count];
            for(var i = 0; i < _asianDates.Count; i++)
            {
                _dateIndexes[i] = dates.GetDateIndex(_asianDates[i]);
            }
        }

        public void Process(PathBlock block)
        {
            for (var path = 0; path < block.NumberOfPaths; path += Vector<double>.Count)
            {
                var steps = block.GetStepsForFactor(path, _assetIndex);
                var finalValues = new Vector<double>(0.0);
                for(var i = 0; i< _dateIndexes.Length;i++)
                {
                    finalValues += steps[_dateIndexes[i]];
                }
                finalValues = (new Vector<double>(_strike)) - (finalValues /  new Vector<double>(_dateIndexes.Length));
                finalValues = Vector.Max(new Vector<double>(0), finalValues);
                _results.Add(finalValues);
            }
        }

        public void SetupFeatures(FeatureCollection pathProcessFeaturesCollection)
        {
            var dates = pathProcessFeaturesCollection.GetFeature<ITimeStepsFeature>();
            dates.AddDates(_asianDates);
        }
    }
}
