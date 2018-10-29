using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Qwack.Core.Basic;
using Qwack.Core.Models;
using Qwack.Math.Interpolation;
using Qwack.Math.Regression;
using Qwack.Paths.Features;
using static System.Math;

namespace Qwack.Paths.Regressors
{
    public class MonoIndexRegressor : IPortfolioValueRegressor
    {
        private const int NSegments = 5;

        private readonly IAssetPathPayoff[] _portfolio;
        private int _nDims;
        private int[] _dateIndexes;
        private double[][] _pathwiseValues;
        private DateTime[] _regressionDates;
        private Currency _repCcy;
        private readonly List<Vector<double>> _results = new List<Vector<double>>();
        private bool _isComplete;

        public MonoIndexRegressor(DateTime[] regressionDates, IAssetPathPayoff[] portfolio, McSettings settings)
        {
            _regressionDates = regressionDates;
            _portfolio = portfolio;
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
            var finalSchedules = _portfolio.Select(x => x.ExpectedFlowsByPath(model));

            for (var d = 0; d < _dateIndexes.Length; d++)
            {
                var exposureDate = _regressionDates[d];
                var sortedFinalValues = new KeyValuePair<double, double>[nPaths];
                foreach (var schedule in finalSchedules)
                {
                    var finalValue = 0.0;
                    for (var p = 0; p < nPaths; p++)
                    {
                        foreach (var flow in schedule[p].Flows)
                        {
                            if (flow.SettleDate > exposureDate)
                                finalValue += flow.Pv;
                        }

                        sortedFinalValues[p] = new KeyValuePair<double, double>(_pathwiseValues[d][p], finalValue);
                    }
                }

                sortedFinalValues = sortedFinalValues.OrderBy(q => q.Key).ToArray();

                var x = new double[NSegments + 1];
                var y = new double[NSegments + 1];
                var samplesPerSegment = nPaths / NSegments;
                for (var i = 0; i < NSegments; i++)
                {
                    var samples = sortedFinalValues.Skip(i * samplesPerSegment).Take(samplesPerSegment);
                    var sampleXs = samples.Select(q => q.Key).ToArray();
                    var sampleYs = samples.Select(q => q.Value).ToArray();
                    var lr = Math.LinearRegression.LinearRegressionVector(sampleXs, sampleYs);
                    var xLo = sampleXs.First();
                    var xHi = sampleXs.Last();
                    var yLo = lr.Alpha + lr.Beta * xLo;
                    var yHi = lr.Alpha + lr.Beta * xHi;

                    if (i == 0)
                    {
                        x[0] = xLo;
                        y[0] = yLo;
                        x[1] = xHi;
                        y[1] = yHi;
                    }
                    else
                    {
                        var targetFunc = new Func<double, double>(q =>
                         {
                             var slope = (q - y[i]) / (xHi - xLo);
                             var err = 0.0;
                             for(var j=0;j<samplesPerSegment;j++)
                             {
                                 var dx = sampleXs[j] - xLo;
                                 var yEst = yLo + slope * dx;
                                 err += (yEst - sampleYs[j]) * (yEst - sampleYs[j]);
                             }
                             return err;
                         });

                        var yHiBetter = Math.Solvers.Newton1D.MethodSolve(targetFunc, yHi, 1e-4);
                        x[i + 1] = xHi;
                        y[i + 1] = yHiBetter;
                    }
                }

                o[d] = InterpolatorFactory.GetInterpolator(x, y, Interpolator1DType.Linear);
            }

            return o;
        }

        public double[] PFE(IAssetFxModel model, double confidenceInterval)
        {
            var o = new double[_dateIndexes.Length];
            var regressors = Regress(model);
            var nPaths = _pathwiseValues.First().Length;
            var targetIx = (int)(nPaths * confidenceInterval);
            for (var d = 0; d < _dateIndexes.Length; d++)
            {
                var exposures = _pathwiseValues[d].Select(p => regressors[d].Interpolate(p)).OrderBy(x => x).ToList();
                o[d] = Max(0.0,exposures[targetIx]);
                o[d] /= model.FundingModel.GetDf(_repCcy, model.BuildDate, _regressionDates[d]);
            }
            return o;
        }
    }
}
