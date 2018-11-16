using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Qwack.Core.Basic;
using Qwack.Core.Models;
using Qwack.Math;
using Qwack.Math.Interpolation;
using Qwack.Math.Regression;
using Qwack.Paths.Features;
using Qwack.Utils.Parallel;
using static System.Math;

namespace Qwack.Paths.Regressors
{
    public class MonoIndexRegressor : IPortfolioValueRegressor
    {
        private const int NSegments = 5;

        private readonly IAssetPathPayoff[] _portfolio;
        private readonly bool _requireContinuity;
        private int _nDims;
        private int[] _dateIndexes;
        private double[][] _pathwiseValues;
        private DateTime[] _regressionDates;
        private Currency _repCcy;
        private readonly List<Vector<double>> _results = new List<Vector<double>>();
        private bool _isComplete;

        public MonoIndexRegressor(DateTime[] regressionDates, IAssetPathPayoff[] portfolio, McSettings settings, bool requireContinuity)
        {
            _regressionDates = regressionDates;
            _portfolio = portfolio;
            _requireContinuity = requireContinuity;
            _repCcy = settings.ReportingCurrency;
            _pathwiseValues = new double[_regressionDates.Length][];
            for (var i = 0; i < _pathwiseValues.Length; i++)
            {
                _pathwiseValues[i] = new double[settings.NumberOfPaths];
            }
        }

        public bool IsComplete => _isComplete;

        public void Finish(IFeatureCollection collection)
        {
            var dims = collection.GetFeature<IPathMappingFeature>();
            _nDims = dims.NumberOfDimensions;
            if (_nDims != 1)
                throw new Exception("Only works for a single regressor");

            var dates = collection.GetFeature<ITimeStepsFeature>();
            _dateIndexes = new int[_regressionDates.Length];
            for (var i = 0; i < _regressionDates.Length; i++)
            {
                _dateIndexes[i] = dates.GetDateIndex(_regressionDates[i]);
            }
            _isComplete = true;
        }

        public void Process(IPathBlock block)
        {
            for (var path = 0; path < block.NumberOfPaths; path += Vector<double>.Count)
            {
                var steps = block.GetStepsForFactor(path, 0);

                var targetIx = 0;
                var nextTarget = _dateIndexes[targetIx];

                for (var s = 0; s < steps.Length; s++)
                {
                    if (s == nextTarget)
                    {
                        for (var v = 0; v < Vector<double>.Count; v++)
                            _pathwiseValues[targetIx][block.GlobalPathIndex + path + v] = steps[s][v];
                        targetIx++;
                        if (targetIx == _dateIndexes.Length)
                            break;
                        nextTarget = _dateIndexes[targetIx];
                    }
                }

            }
        }

        public void SetupFeatures(IFeatureCollection pathProcessFeaturesCollection)
        {
            var dates = pathProcessFeaturesCollection.GetFeature<ITimeStepsFeature>();
            dates.AddDates(_regressionDates);
        }

        public IInterpolator1D[] Regress(IAssetFxModel model)
        {
            var o = new IInterpolator1D[_dateIndexes.Length];

            var nPaths = _pathwiseValues.First().Length;
            var finalSchedules = _portfolio.Select(x => x.ExpectedFlowsByPath(model)).ToArray();

            ParallelUtils.Instance.For(0, _dateIndexes.Length, 1, d =>
            {
                var exposureDate = _regressionDates[d];
                var sortedFinalValues = new KeyValuePair<double, double>[nPaths];

                for (var p = 0; p < nPaths; p++)
                {
                    var finalValue = 0.0;
                    foreach (var schedule in finalSchedules)
                    {
                        foreach (var flow in schedule[p].Flows)
                        {
                            if (flow.SettleDate > exposureDate)
                                finalValue += flow.Pv;
                        }
                    }
                    sortedFinalValues[p] = new KeyValuePair<double, double>(_pathwiseValues[d][p], finalValue);
                }

                //DumpDataToDisk(sortedFinalValues,$@"C:\temp\regData{d}.csv");

                sortedFinalValues = sortedFinalValues.OrderBy(q => q.Key).ToArray();
                
                if (_requireContinuity)
                    o[d] = SegmentedLinearRegression.Regress(sortedFinalValues.Select(x => x.Key).ToArray(), sortedFinalValues.Select(y => y.Value).ToArray(), 5);
                else
                    o[d] = SegmentedLinearRegression.RegressNotContinuous(sortedFinalValues.Select(x => x.Key).ToArray(), sortedFinalValues.Select(y => y.Value).ToArray(), 5);

            }).Wait();

            return o;
        }

        public double[] PFE(IAssetFxModel model, double confidenceInterval)
        {
            var o = new double[_dateIndexes.Length];
            var regressors = Regress(model);
            var nPaths = _pathwiseValues.First().Length;
            var targetIx = (int)(nPaths * confidenceInterval);

            ParallelUtils.Instance.For(0, _dateIndexes.Length, 1, d =>
            {
                var exposures = _pathwiseValues[d].Select(p => regressors[d].Interpolate(p)).OrderBy(x => x).ToList();
                o[d] = Max(0.0, exposures[targetIx]);
                o[d] /= model.FundingModel.GetDf(_repCcy, model.BuildDate, _regressionDates[d]);
            }).Wait();

            return o;
        }

        private void DumpDataToDisk(KeyValuePair<double, double>[] data, string fileName)
        {
            var linesToWrite = data.Select(x => string.Join(",", x.Key, x.Value)).ToArray();
            System.IO.File.WriteAllLines(fileName, linesToWrite);
        }
    }
}
