using System;
using System.Collections.Generic;
using System.Numerics;
using Qwack.Core.Models;
using Qwack.Paths.Features;

namespace Qwack.Paths.Processes
{
    public class SimpleAveragePathCalculator : IPathProcess
    {
        private readonly string _name;
        private int _factorIndex;

        private readonly bool _isComplete;
        public bool CompactMode { get; set; }

        public Dictionary<int, double[]> PathSumsByBlock { get; private set; }
        public Dictionary<int, int> PathCountsByBlock { get; private set; }

        public SimpleAveragePathCalculator(string name) => _name = name;

        public bool IsComplete => _isComplete;

        private readonly object _threadLock = new();
        private void SetupAverages(Span<Vector<double>> blockSteps)
        {
            if (PathSumsByBlock == null)
            {
                lock (_threadLock)
                {
                    if (PathSumsByBlock == null)
                    {
                        PathSumsByBlock = new Dictionary<int, double[]>();
                        PathCountsByBlock = new Dictionary<int, int>();
                    }
                }
            }
        }

        public void Process(IPathBlock block)
        {
            for (var path = 0; path < block.NumberOfPaths; path += Vector<double>.Count)
            {
                var steps = block.GetStepsForFactor(path, _factorIndex);
                SetupAverages(steps);

                if (!PathSumsByBlock.ContainsKey(block.GlobalPathIndex))
                {
                    PathSumsByBlock.Add(block.GlobalPathIndex, new double[steps.Length]);
                    PathCountsByBlock.Add(block.GlobalPathIndex, block.NumberOfPaths);
                }

                for (var i = 0; i < steps.Length; i++)
                {
                    for (var j = 0; j < Vector<double>.Count; j++)
                        PathSumsByBlock[block.GlobalPathIndex][i] += steps[i][j];
                }
            }
        }

        public void SetupFeatures(IFeatureCollection pathProcessFeaturesCollection)
        {
            var mappingFeature = pathProcessFeaturesCollection.GetFeature<IPathMappingFeature>();
            _factorIndex = mappingFeature.GetDimension(_name);
        }
    }
}

