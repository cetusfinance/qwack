using Qwack.Options.VolSurfaces;
using Qwack.Paths.Features;
using System;
using System.Collections.Generic;
using System.Numerics;
using Qwack.Math.Extensions;
using System.Linq;
using Qwack.Core.Models;
using Qwack.Serialization;
using static System.Math;

namespace Qwack.Paths.Processes
{
    public class SimpleAveragePathCalculator : IPathProcess
    {
        private string _name;
        private int _factorIndex;
        private int _nPaths;
        private bool _isComplete;
        public bool CompactMode { get; set; }

        public double[] PathSum { get; private set; }
        public double[] PathAvg => PathSum.Select(x => x / _nPaths).ToArray();

        public Dictionary<int,double[]> PathSumsByBlock { get; private set; }
        public Dictionary<int, int> PathCountsByBlock { get; private set; }

        public SimpleAveragePathCalculator(string name)
        {
            _name = name;
        }

        public bool IsComplete => _isComplete;

        private object _threadLock = new object();
        private void SetupAverages(Span<Vector<double>> blockSteps)
        {
            if (CompactMode)
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
            else
            {
                if (PathSum == null)
                {
                    lock (_threadLock)
                    {
                        if (PathSum == null)
                        {
                            PathSum = new double[blockSteps.Length];
                        }
                    }
                }
            }
        }

        public void Process(IPathBlock block)
        {
            if (CompactMode)
            {
                for (var path = 0; path < block.NumberOfPaths; path += Vector<double>.Count)
                {
                    var steps = block.GetStepsForFactor(path, _factorIndex);
                    SetupAverages(steps);

                    if (CompactMode)
                    {
                        if (!PathSumsByBlock.ContainsKey(block.GlobalPathIndex))
                        {
                            PathSumsByBlock.Add(block.GlobalPathIndex, new double[steps.Length]);
                            PathCountsByBlock.Add(block.GlobalPathIndex, steps.Length);
                        }

                        for (var i = 0; i < steps.Length; i++)
                        {
                            for (var j = 0; j < Vector<double>.Count; j++)
                                PathSumsByBlock[block.GlobalPathIndex][i] += steps[i][j];
                        }
                    }
                    else
                    {
                        for (var i = 0; i < steps.Length; i++)
                        {
                            for (var j = 0; j < Vector<double>.Count; j++)
                                lock (_threadLock)
                                {
                                    PathSum[i] += steps[i][j];
                                }
                        }
                    }
                   
                }
            }
            else
            {
                for (var path = 0; path < block.NumberOfPaths; path += Vector<double>.Count)
                {
                    var steps = block.GetStepsForFactor(path, _factorIndex);
                    SetupAverages(steps);

                    for (var i = 0; i < steps.Length; i++)
                    {
                        for (var j = 0; j < Vector<double>.Count; j++)
                            lock (_threadLock)
                            {
                                PathSum[i] += steps[i][j];
                            }
                    }
                }
            }
        }

        public void SetupFeatures(IFeatureCollection pathProcessFeaturesCollection)
        {
            var mappingFeature = pathProcessFeaturesCollection.GetFeature<IPathMappingFeature>();
            _factorIndex = mappingFeature.GetDimension(_name);

            var engine = pathProcessFeaturesCollection.GetFeature<IEngineFeature>();
            _nPaths = engine.NumberOfPaths;
        }
    }
}

