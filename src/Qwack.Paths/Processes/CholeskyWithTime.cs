using Qwack.Paths.Features;
using System;
using System.Collections.Generic;
using System.Numerics;
using Qwack.Math.Matrix;
using Qwack.Core.Models;
using Qwack.Options;
using System.Linq;
using Qwack.Core.Basic.Correlation;

namespace Qwack.Paths.Processes
{
    public class CholeskyWithTime : IPathProcess, IRequiresFinish
    {
        private bool _isComplete;
        
        private ICorrelationMatrix _matrix;
        private IAssetFxModel _model;
        private List<CorrelationMatrix> _localMatrix;
        private List<double[][]> _localMatrixSquare;
        private List<double[][]> _decompMatrix;
        private readonly Vector<double> _two = new Vector<double>(2.0);


        public CholeskyWithTime(ICorrelationMatrix correlationMatrix, IAssetFxModel model)
        {
            _matrix = correlationMatrix;
            _model = model;
        }

        public bool IsComplete => _isComplete;

        public void Finish(IFeatureCollection collection)
        {
            var times = collection.GetFeature<ITimeStepsFeature>().Times;
            _localMatrix = _model.LocalCorrelationObjects(times);
            //var localMatrixObjects = _localMatrix.Select(x=>new CorrelationMatrix()
            var pathFeatures = collection.GetFeature<IPathMappingFeature>();
            _localMatrixSquare = new List<double[][]>();
            var dimNames = pathFeatures.GetDimensionNames();
            
            for (var t = 0; t < _localMatrix.Count; t++)
            {
                _localMatrixSquare.Add(new double[pathFeatures.NumberOfDimensions][]);
                for (var i = 0; i < _localMatrixSquare[t].Length; i++)
                {
                    _localMatrixSquare[t][i] = new double[pathFeatures.NumberOfDimensions];
                    for (var j = 0; j < _localMatrixSquare[t][i].Length; j++)
                    {
                        _localMatrixSquare[t][i][j] = i == j ? 1.0 : (_localMatrix[t].TryGetCorrelation(dimNames[i], dimNames[j], out var correl) ? correl : 0.0);
                    }
                }
            }

            _decompMatrix = _localMatrixSquare.Select(l => l.Cholesky2()).ToList();
            _isComplete = true;
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
                    var correlatedRands = Correlate(randsForThisStep,step);
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
