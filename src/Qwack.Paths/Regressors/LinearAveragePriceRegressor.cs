using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Qwack.Core.Models;
using Qwack.Math;
using Qwack.Math.Interpolation;
using Qwack.Paths.Features;


namespace Qwack.Paths.Regressors
{
    public class LinearAveragePriceRegressor : IForwardPriceEstimate
    {
        private int _nDims;
        private int _dateIxRegression;
        private int[] _dateIxsFwd;
        private int[] _assetIxs;
        private double[] _pathwiseValuesReg;
        private double[][] _pathwiseValuesFwd;
        private double[] _pathwiseFwdsSingleDim;
        private readonly DateTime _regressionDate;
        private readonly DateTime[] _fixingDates;
        private readonly string _regressionKey;
        private readonly List<Vector<double>> _results = new();

        public IInterpolator1D Regressor { get; private set; }
        private readonly object _threadlock = new();

        public LinearAveragePriceRegressor(DateTime regressionDate, DateTime[] fixingDates, string regressionKey)
        {
            _regressionDate = regressionDate;
            _fixingDates = fixingDates;
            _regressionKey = regressionKey;
        }

        public bool IsComplete { get; private set; }

        public void Finish(IFeatureCollection collection)
        {
            var dims = collection.GetFeature<IPathMappingFeature>();


            var assetNames = _regressionKey.Split('*');
            _assetIxs = assetNames.Select(x => dims.GetDimension(x)).ToArray();
            _nDims = _assetIxs.Length;
            if (_nDims > 2) throw new InvalidOperationException("Only supports Single Asset and FX");

            var dates = collection.GetFeature<ITimeStepsFeature>();
            _dateIxRegression = dates.GetDateIndex(_regressionDate);
            _dateIxsFwd = _fixingDates.Select(d => dates.GetDateIndex(d)).ToArray();

            var engine = collection.GetFeature<IEngineFeature>();

            _pathwiseValuesReg = new double[engine.NumberOfPaths];

            if (_nDims > 1)
            {
                _pathwiseValuesFwd = new double[engine.NumberOfPaths][];

                for (var i = 0; i < _pathwiseValuesFwd.Length; i++)
                    _pathwiseValuesFwd[i] = new double[_fixingDates.Length];
            }
            else
            {
                _pathwiseFwdsSingleDim = new double[engine.NumberOfPaths];
            }
            IsComplete = true;
        }

        private void ProcessMultiDim(IPathBlock block)
        {
            for (var path = 0; path < block.NumberOfPaths; path += Vector<double>.Count)
            {
                for (var d = 0; d < _nDims; d++)
                {
                    var steps = block.GetStepsForFactor(path, _assetIxs[d]);
                    if (d == 0)
                    {
                        for (var fd = 0; fd < _dateIxsFwd.Length; fd++)
                        {
                            for (var v = 0; v < Vector<double>.Count; v++)
                            {

                                _pathwiseValuesFwd[block.GlobalPathIndex + path + v][fd] = steps[_dateIxsFwd[fd]][v];
                                _pathwiseValuesReg[block.GlobalPathIndex + path + v] = steps[_dateIxRegression][v];
                            }
                        }
                    }
                    else
                    {
                        for (var fd = 0; fd < _dateIxsFwd.Length; fd++)
                        {
                            for (var v = 0; v < Vector<double>.Count; v++)
                            {

                                _pathwiseValuesFwd[block.GlobalPathIndex + path + v][fd] *= steps[_dateIxsFwd[fd]][v];
                                _pathwiseValuesReg[block.GlobalPathIndex + path + v] *= steps[_dateIxRegression][v];

                            }
                        }
                    }
                }
            }
        }

        private void ProcessSingleDim(IPathBlock block)
        {
            var dateForwardLength  = _dateIxsFwd.Length;
            for (var path = 0; path < block.NumberOfPaths; path += Vector<double>.Count)
            {
                var steps = block.GetStepsForFactor(path, _assetIxs[0]);

                for (var fd = 0; fd < dateForwardLength; fd++)
                {
                    for (var v = 0; v < Vector<double>.Count; v++)
                    {

                        _pathwiseFwdsSingleDim[block.GlobalPathIndex + path + v] += (steps[_dateIxsFwd[fd]][v] / dateForwardLength);
                        _pathwiseValuesReg[block.GlobalPathIndex + path + v] = steps[_dateIxRegression][v];
                    }
                }
            }
        }

        public void Process(IPathBlock block)
        {
            if (_nDims > 1) ProcessMultiDim(block);
            else ProcessSingleDim(block);
        }

        public void SetupFeatures(IFeatureCollection pathProcessFeaturesCollection)
        {
            var dates = pathProcessFeaturesCollection.GetFeature<ITimeStepsFeature>();
            dates.AddDates(_fixingDates);
            dates.AddDates(new[] { _regressionDate });
        }

        public IInterpolator1D PerformRegression()
        {
            if (_nDims > 1)
            {
                var nPaths = _pathwiseValuesFwd.Length;
                var pathAvgs = _pathwiseValuesFwd.Select(x => x.Average()).ToArray();

                if (_pathwiseValuesReg.All(x => x == _pathwiseValuesReg.First()) && pathAvgs.All(x => x == pathAvgs.First()))
                    return new ParametricLinearInterpolator(pathAvgs.First(), 0.0);

                var lr = LinearRegression.LinearRegressionVector(_pathwiseValuesReg, pathAvgs);
                return new ParametricLinearInterpolator(lr.Alpha, lr.Beta);
            }
            else
            {
                var lr = LinearRegression.LinearRegressionVector(_pathwiseValuesReg, _pathwiseFwdsSingleDim);
                return new ParametricLinearInterpolator(lr.Alpha, lr.Beta);
            }
        }

        public double Predict(double spot)
        {
            if (Regressor == null)
            {
                lock (_threadlock)
                {
                    if (Regressor == null)
                    {
                        Regressor = PerformRegression();
                    }
                }
            }

            return Regressor.Interpolate(spot);
        }

        public double GetEstimate(double? spot, int? globalPathIndex) => Predict(spot ?? 0);

        public override bool Equals(object obj) => obj is LinearAveragePriceRegressor regressor &&
                   _regressionDate == regressor._regressionDate &&
                   Enumerable.SequenceEqual(_fixingDates, regressor._fixingDates) &&
                   _regressionKey == regressor._regressionKey;

    }
}
