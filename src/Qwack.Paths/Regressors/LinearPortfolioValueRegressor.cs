using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Qwack.Core.Basic;
using Qwack.Core.Models;
using Qwack.Math.Regression;
using Qwack.Paths.Features;
using static System.Math;

namespace Qwack.Paths.Regressors
{
    public class LinearPortfolioValueRegressor : IPathProcess, IRequiresFinish
    {

        private readonly IAssetPathPayoff[] _portfolio;
        private int _nDims;
        private int[] _dateIndexes;
        private double[][][] _pathwiseValues;
        private DateTime[] _regressionDates;
        private Currency _repCcy;
        private readonly List<Vector<double>> _results = new List<Vector<double>>();
        private bool _isComplete;

        public LinearPortfolioValueRegressor(DateTime[] regressionDates, IAssetPathPayoff[] portfolio, McSettings settings)
        {
            _regressionDates = regressionDates;
            _portfolio = portfolio;
            _repCcy = settings.ReportingCurrency;
            _pathwiseValues = new double[_regressionDates.Length][][];
            for (var i = 0; i < _pathwiseValues.Length; i++)
            {
                _pathwiseValues[i] = new double[settings.NumberOfPaths][];
            }
        }

        public bool IsComplete => _isComplete;

        public void Finish(IFeatureCollection collection)
        {
            var dims = collection.GetFeature<IPathMappingFeature>();
            _nDims = dims.NumberOfDimensions;
            for (var i = 0; i < _pathwiseValues.Length; i++)
                for (var j = 0; j < _pathwiseValues[i].Length; j++)
                    _pathwiseValues[i][j] = new double[_nDims];

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
                for (var d = 0; d < _nDims; d++)
                {
                    var steps = block.GetStepsForFactor(path, d);

                    var targetIx = 0;
                    var nextTarget = _dateIndexes[targetIx];

                    for (var s = 0; s < steps.Length; s++)
                    {
                        if (s == nextTarget)
                        {
                            for (var v = 0; v < Vector<double>.Count; v++)
                                _pathwiseValues[targetIx][block.GlobalPathIndex + path + v][d] = steps[s][v];
                            targetIx++;
                            if (targetIx == _dateIndexes.Length)
                                break;
                            nextTarget = _dateIndexes[targetIx];
                        }
                    }
                }
            }
        }

        public void SetupFeatures(IFeatureCollection pathProcessFeaturesCollection)
        {
            var dates = pathProcessFeaturesCollection.GetFeature<ITimeStepsFeature>();
            dates.AddDates(_regressionDates);
        }

        public MultipleLinearRegressor[] Regress(IAssetFxModel model)
        {
            var nPaths = _pathwiseValues.First().Length;
            var finalSchedules = _portfolio.Select(x => x.ExpectedFlowsByPath(model));
            var finalValues = new double[_dateIndexes.Length][];
            for (var d = 0; d < finalValues.GetLength(0); d++)
            {
                var exposureDate = _regressionDates[d];
                finalValues[d] = new double[nPaths];
                foreach (var schedule in finalSchedules)
                {
                    for (var p = 0; p < finalValues[d].Length; p++)
                    {
                        foreach (var flow in schedule[p].Flows)
                        {
                            if (flow.SettleDate > exposureDate)
                                finalValues[d][p] += flow.Pv;
                        }
                    }
                }
            }

            var o = new MultipleLinearRegressor[_dateIndexes.Length];
            for (var d = 0; d < _dateIndexes.Length; d++)
            {
                if (_regressionDates[d] <= model.BuildDate)
                {
                    o[d] = new MultipleLinearRegressor(new double[_pathwiseValues[d][0].Length + 1]);
                }
                else
                {
                    var mlr = MultipleLinearRegression.RegressHistorical(_pathwiseValues[d], finalValues[d]);
                    o[d] = new MultipleLinearRegressor(mlr);
                }
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
                var exposures = _pathwiseValues[d].Select(p => regressors[d].Regress(p)).OrderBy(x => x).ToList();
                o[d] = Max(0.0,exposures[targetIx]);
                o[d] /= model.FundingModel.GetDf(_repCcy, model.BuildDate, _regressionDates[d]);
            }
            return o;
        }
    }
}
