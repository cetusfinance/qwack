using Qwack.Paths.Features;
using System;
using System.Numerics;
using Qwack.Math.Matrix;
using Qwack.Core.Models;

namespace Qwack.Paths.Processes
{
    public class Cholesky : IPathProcess, IRequiresFinish
    {
        private bool _isComplete;
        //private readonly string _name = "Cholesky";
        private double[][] _correlationMatrix;
        private readonly ICorrelationMatrix _matrix;
        private double[][] _decompMatrix;
        private readonly Vector<double> _two = new(2.0);
        public Cholesky(double[][] correlationMatrix)
        {
            if (correlationMatrix.MaxAbsElement() > 1.0)
                throw new Exception("Invalid correlation, must be in the range -1.0 to +1.0");
            _correlationMatrix = correlationMatrix;
        }
        public Cholesky(ICorrelationMatrix correlationMatrix)
        {
            _matrix = correlationMatrix;
        }
        public bool IsComplete => _isComplete;
        public void Finish(IFeatureCollection collection)
        {
            if (_matrix != null)
            {
                var pathFeatures = collection.GetFeature<IPathMappingFeature>();
                _correlationMatrix = new double[pathFeatures.NumberOfDimensions][];
                var dimNames = pathFeatures.GetDimensionNames();
                for (var i = 0; i < _correlationMatrix.Length; i++)
                {
                    _correlationMatrix[i] = new double[pathFeatures.NumberOfDimensions];
                    for (var j = 0; j < _correlationMatrix[i].Length; j++)
                    {
                        _correlationMatrix[i][j] = i == j ? 1.0 : (_matrix.TryGetCorrelation(dimNames[i], dimNames[j], out var correl) ? correl : 0.0);
                    }
                }
            }
            _decompMatrix = _correlationMatrix.Cholesky2();
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
                    var correlatedRands = Correlate(randsForThisStep);
                    for (var r = 0; r < randsForSteps.Length; r++)
                    {
                        var x = block.GetStepsForFactor(path, r);
                        x[step] = correlatedRands[r];
                    }
                }
            }
        }
        private double[] Correlate(double[] rands)
        {
            var returnValues = new double[rands.Length];
            for (var y = 0; y < returnValues.Length; y++)
            {
                for (var x = 0; x <= y; x++)
                {
                    returnValues[y] += rands[x] * _decompMatrix[y][x];
                }
            }
            return returnValues;
        }
        private Vector<double>[] Correlate(Vector<double>[] rands)
        {
            var returnValues = new Vector<double>[rands.Length];
            for (var y = 0; y < returnValues.Length; y++)
            {
                for (var x = 0; x <= y; x++)
                {
                    returnValues[y] += rands[x] * _decompMatrix[y][x];
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
