using Qwack.Options.VolSurfaces;
using Qwack.Paths.Features;
using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Core.Underlyings;
using System.Numerics;
using Qwack.Math.Extensions;
using Qwack.Math.Interpolation;
using Qwack.Math.Matrix;

namespace Qwack.Paths.Processes
{
    public class Cholesky : IPathProcess, IRequiresFinish
    {
        private bool _isComplete;

        private string _name = "Cholesky";
        private double[][] _decompMatrix;


        private readonly Vector<double> _two = new Vector<double>(2.0);

        public Cholesky(double[][] correlationMatrix)
        {
            if (correlationMatrix.MaxAbsElement() > 1.0)
                throw new Exception("Invalid correlation, must be in the range -1.0 to +1.0");

            _decompMatrix = correlationMatrix.Cholesky();
        }

        public bool IsComplete => _isComplete;

        public void Finish(FeatureCollection collection)
        {
            _isComplete = true;
        }

        public void Process(PathBlock block)
        {
            var nFactors = block.Factors;

            for (var path = 0; path < block.NumberOfPaths; path += Vector<double>.Count)
            {
                var randsForSteps = new Vector<double>[nFactors][];
                for(var r=0;r< randsForSteps.Length;r++)
                {
                    randsForSteps[r] = block.GetStepsForFactor(path, r).ToArray();
                }

                for (var step = 1; step < block.NumberOfSteps; step++)
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

        public void SetupFeatures(FeatureCollection pathProcessFeaturesCollection)
        {
            var mappingFeature = pathProcessFeaturesCollection.GetFeature<IPathMappingFeature>();
        }
    }
}
