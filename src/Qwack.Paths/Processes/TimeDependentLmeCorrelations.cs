using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Qwack.Core.Basic.Correlation;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Math.Matrix;
using Qwack.Options;
using Qwack.Paths.Features;

namespace Qwack.Paths.Processes
{
    public class TimeDependentLmeCorrelations : IPathProcess, IRequiresFinish
    {
        private bool _isComplete;

        private readonly IAssetFxModel _model;
        private readonly ICalendarProvider _calendar;
        private readonly double _lambda;
        private List<CorrelationMatrix> _localMatrix;
        private List<double[][]> _localMatrixSquare;
        private List<double[][]> _decompMatrix;
        private readonly Vector<double> _two = new(2.0);

        private Dictionary<string, DateTime?> _prompts;


        public TimeDependentLmeCorrelations(IAssetFxModel model, ICalendarProvider calendar, double lambda = 0.01)
        {
            _model = model;
            _calendar = calendar;
            _lambda = lambda;
        }

        public bool IsComplete => _isComplete;

        public void Initialize(Dictionary<string, DateTime?> prompts)
        {
            _prompts = prompts;
        }

        public void Finish(IFeatureCollection collection)
        {
            var times = collection.GetFeature<ITimeStepsFeature>().Times;
            var dates = collection.GetFeature<ITimeStepsFeature>().Dates;
            //_localMatrix = _model.LocalCorrelationObjects(times);
            //var localMatrixObjects = _localMatrix.Select(x=>new CorrelationMatrix()
            var pathFeatures = collection.GetFeature<IPathMappingFeature>();
            _localMatrixSquare = new List<double[][]>();
            var dimNames = pathFeatures.GetDimensionNames();
            
            for (var t = 0; t < times.Length; t++)
            {
                _localMatrixSquare.Add(new double[pathFeatures.NumberOfDimensions][]);
                for (var i = 0; i < _localMatrixSquare[t].Length; i++)
                {
                    _localMatrixSquare[t][i] = new double[pathFeatures.NumberOfDimensions];
                    for (var j = 0; j < _localMatrixSquare[t][i].Length; j++)
                    {
                        _localMatrixSquare[t][i][j] = i == j ? 1.0 : GetCorrelation(dates[t], dimNames[i], dimNames[j]);// i == j ? 1.0 : (_localMatrix[t].TryGetCorrelation(dimNames[i], dimNames[j], out var correl) ? correl : 0.0);
                    }
                }
            }

            _decompMatrix = _localMatrixSquare.Select(l => l.Cholesky2()).ToList();
            _isComplete = true;
        }

        private double GetCorrelation(DateTime t, string A, string B)
        {
            var promptA = _prompts[A] ?? t.SpotDate(2.Bd(), _calendar.GetCalendar("GBP"), _calendar.GetCalendar("USD"));
            var promptB = _prompts[B] ?? t.SpotDate(2.Bd(), _calendar.GetCalendar("GBP"), _calendar.GetCalendar("USD"));

            var deltaT = System.Math.Abs((promptB - promptA).TotalDays / 365.0);

            return System.Math.Exp(-_lambda * deltaT * deltaT);
        }

        public void Process(IPathBlock block)
        {
            var nFactors = block.Factors;
            for (var path = 0; path < block.NumberOfPaths; path += Vector<double>.Count)
            {
                var randsForSteps = new Vector<double>[nFactors][];
                for (var r = 0; r < randsForSteps.Length; r++)
                {
                    randsForSteps[r] = block.GetStepsForFactor(path, r).ToArray();
                }
                for (var step = 0; step < block.NumberOfSteps; step++)
                {
                    var randsForThisStep = new Vector<double>[nFactors];
                    for (var r = 0; r < randsForThisStep.Length; r++)
                    {
                        randsForThisStep[r] = randsForSteps[r][step];
                    }
                    var correlatedRands = Correlate(randsForThisStep, step);
                    for (var r = 0; r < randsForSteps.Length; r++)
                    {
                        var x = block.GetStepsForFactor(path, r);
                        x[step] = correlatedRands[r];
                    }
                }
            }
        }
        private double[] Correlate(double[] rands, int timestep)
        {
            var returnValues = new double[rands.Length];
            for (var y = 0; y < returnValues.Length; y++)
            {
                for (var x = 0; x <= y; x++)
                {
                    returnValues[y] += rands[x] * _decompMatrix[timestep][y][x];
                }
            }
            return returnValues;
        }
        private Vector<double>[] Correlate(Vector<double>[] rands, int timestep)
        {
            var returnValues = new Vector<double>[rands.Length];
            for (var y = 0; y < returnValues.Length; y++)
            {
                for (var x = 0; x <= y; x++)
                {
                    returnValues[y] += rands[x] * _decompMatrix[timestep][y][x];
                }
            }
            return returnValues;
        }
        public void SetupFeatures(IFeatureCollection pathProcessFeaturesCollection)
        {
            var mappingFeature = pathProcessFeaturesCollection.GetFeature<IPathMappingFeature>();
        }
    }
}
