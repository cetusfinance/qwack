using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Qwack.Core.Models;
using Qwack.Paths.Features;

namespace Qwack.Paths.Payoffs
{
    public class AsianPut : IPathProcess, IRequiresFinish
    {
        private List<DateTime> _asianDates;
        private readonly double _strike;
        private readonly string _assetName;
        private int _assetIndex;
        private int[] _dateIndexes;
        private List<Vector<double>> _results = new List<Vector<double>>();
        private bool _isComplete;

        public AsianPut(string assetName, double strike, List<DateTime> asianingDates)
        {
            _asianDates = asianingDates;
            _assetName = assetName;
            _strike = strike;
        }

        public bool IsComplete => _isComplete;

        public void Finish(IFeatureCollection collection)
        {
            var dims = collection.GetFeature<IPathMappingFeature>();
            _assetIndex = dims.GetDimension(_assetName);

            var dates = collection.GetFeature<ITimeStepsFeature>();
            _dateIndexes = new int[_asianDates.Count];
            for(var i = 0; i < _asianDates.Count; i++)
            {
                _dateIndexes[i] = dates.GetDateIndex(_asianDates[i]);
            }
            _isComplete = true;
        }

        public void Process(IPathBlock block)
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

        public void SetupFeatures(IFeatureCollection pathProcessFeaturesCollection)
        {
            var dates = pathProcessFeaturesCollection.GetFeature<ITimeStepsFeature>();
            dates.AddDates(_asianDates);
        }
    }
}
