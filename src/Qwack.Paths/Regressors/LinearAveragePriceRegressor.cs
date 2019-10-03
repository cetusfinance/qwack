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
    public class LinearAveragePriceRegressor : IPathProcess, IRequiresFinish
    {
        private int _nDims;
        private int _dateIxRegression;
        private int[] _dateIxsFwd;
        private int[] _assetIxs;
        private double[] _pathwiseValuesReg;
        private double[][] _pathwiseValuesFwd;
        private DateTime _regressionDate;
        private DateTime[] _fixingDates;
        private string _regressionKey;
        private readonly List<Vector<double>> _results = new List<Vector<double>>();

        public IInterpolator1D Regressor { get; private set; }
        private object _threadlock = new object();

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
            
            var dates = collection.GetFeature<ITimeStepsFeature>();
            _dateIxRegression = dates.GetDateIndex(_regressionDate);
            _dateIxsFwd = _fixingDates.Select(d=>dates.GetDateIndex(d)).ToArray();

            var engine = collection.GetFeature<IEngineFeature>();

            _pathwiseValuesReg = new double[engine.NumberOfPaths];
            _pathwiseValuesFwd = new double[engine.NumberOfPaths][];

            for (var i = 0; i < _pathwiseValuesFwd.Length; i++)
                _pathwiseValuesFwd[i] = new double[_fixingDates.Length];

            IsComplete = true;
        }

        public void Process(IPathBlock block)
        {
            for (var path = 0; path < block.NumberOfPaths; path += Vector<double>.Count)
            {
                for (var d = 0; d < _nDims; d++)
                {
                    var steps = block.GetStepsForFactor(path, _assetIxs[d]);

                    for (var fd = 0; fd < _dateIxsFwd.Length; fd++)
                    {
                        for (var v = 0; v < Vector<double>.Count; v++)
                        {
                            if (d == 0)
                            {
                                _pathwiseValuesFwd[block.GlobalPathIndex + path + v][fd] = steps[_dateIxsFwd[fd]][v];
                                _pathwiseValuesReg[block.GlobalPathIndex + path + v] = steps[_dateIxRegression][v];
                            }
                            else
                            {
                                _pathwiseValuesFwd[block.GlobalPathIndex + path + v][fd] *= steps[_dateIxsFwd[fd]][v];
                                _pathwiseValuesReg[block.GlobalPathIndex + path + v] *= steps[_dateIxRegression][v];
                            }
                        }
                    }
                }
            }
        }

        public void SetupFeatures(IFeatureCollection pathProcessFeaturesCollection)
        {
            var dates = pathProcessFeaturesCollection.GetFeature<ITimeStepsFeature>();
            dates.AddDates(_fixingDates);
            dates.AddDates(new[] { _regressionDate });
        }

        public IInterpolator1D PerformRegression()
        {
            var nPaths = _pathwiseValuesFwd.Length;
            var pathAvgs = _pathwiseValuesFwd.Select(x => x.Average()).ToArray();

            if (_pathwiseValuesReg.All(x => x == _pathwiseValuesReg.First()) && pathAvgs.All(x => x == pathAvgs.First()))
                return new ParametricLinearInterpolator(pathAvgs.First(), 0.0);

            var lr = LinearRegression.LinearRegressionVector(_pathwiseValuesReg, pathAvgs);
            return new ParametricLinearInterpolator(lr.Alpha, lr.Beta);
        }

        public double Predict(double spot)
        {
            if(Regressor==null)
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

        public override bool Equals(object obj) => obj is LinearAveragePriceRegressor regressor &&
                   _regressionDate == regressor._regressionDate &&
                   Enumerable.SequenceEqual(_fixingDates, regressor._fixingDates) &&
                   _regressionKey == regressor._regressionKey;
    }
}
