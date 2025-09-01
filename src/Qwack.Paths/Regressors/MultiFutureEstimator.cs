using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Options;
using Qwack.Paths.Features;

namespace Qwack.Paths.Regressors
{
    public class MultiFutureEstimator(string assetId, DateTime valDate, DateTime[] averageDates, ICalendarProvider calendarProvider, DateShifter dateShifter = null) : IForwardPriceEstimate
    {

        private Dictionary<DateTime, int> _dimDict;
        private Dictionary<int, double> _weightsByDim;
        private double[] _avgsByPath;
        private int _valDateIx;

        private double _carry;

        public bool IsComplete => true;

        public void Finish(IFeatureCollection collection) 
        {
            var dims = collection.GetFeature<IPathMappingFeature>();

            var dimNames = dims.GetDimensionNames().Where(x=>x.StartsWith(assetId)).ToList();
            _dimDict = [];
            foreach (var dim in dimNames)
            {
                var dimIx = dims.GetDimension(dim);
                if (dim == assetId)
                    _dimDict.Add(valDate.SpotDate(2.Bd(), calendarProvider.GetCalendarSafe("GBP"), calendarProvider.GetCalendarSafe("USD")), dimIx);
                else
                {
                    var dt = DateTime.ParseExact(dim.Split("~")[1], "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);
                    _dimDict.Add(dt, dimIx);
                }
            }

            var pillars = _dimDict.Keys.OrderBy(x=>x).ToArray();
            var totalWeights = new Dictionary<int, double>();
            var totalWeightsByDim = new Dictionary<int, double>();
            foreach (var d in averageDates)
            {
                var w = LMEFunctions.LinearWeights(pillars, d);
                foreach(var kvp in w)
                {
                    if (!totalWeights.ContainsKey(kvp.Key))
                        totalWeights[kvp.Key] = 0.0;
                    totalWeights[kvp.Key] += kvp.Value;

                    var dimIx = _dimDict[pillars[kvp.Key]];
                    if (!totalWeightsByDim.ContainsKey(dimIx))
                        totalWeightsByDim[dimIx] = 0.0;
                    totalWeightsByDim[dimIx] += kvp.Value;
                }
            }
            _weightsByDim = totalWeightsByDim.ToDictionary(x => x.Key, x => x.Value / averageDates.Length);

            var engine = collection.GetFeature<IEngineFeature>();
            _avgsByPath = new double[engine.NumberOfPaths];

            _valDateIx = collection.GetFeature<ITimeStepsFeature>().GetDateIndex(valDate);
        }
        public double GetEstimate(double? spot, int? globalPathIndex) => _avgsByPath[globalPathIndex.Value];
        public void Process(IPathBlock block) 
        {
            for (var path = 0; path < block.NumberOfPaths; path += Vector<double>.Count)
            {
                foreach(var kvp in _weightsByDim)
                {
                    var steps = block.GetStepsForFactor(path, kvp.Key);
                    for (var v = 0; v < Vector<double>.Count; v++)
                    {
                        _avgsByPath[block.GlobalPathIndex + path + v] += (steps[_valDateIx][v] * kvp.Value);
                    }
                }
            }
        }
        public void SetupFeatures(IFeatureCollection pathProcessFeaturesCollection) { }
    }
}
