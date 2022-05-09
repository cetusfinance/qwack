using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Qwack.Core.Models;
using Qwack.Math;
using Qwack.Paths.Features;

namespace Qwack.Paths.Payoffs
{
    public class Correlation : IPathProcess, IRequiresFinish, IRunSingleThreaded
    {
        private readonly string _assetName1;
        private readonly string _assetName2;
        private int _assetIndex1;
        private int _assetIndex2;

        private readonly List<Vector<double>> _results = new();
        private bool _isComplete;

        public Correlation(string assetName1, string assetName2)
        {
            _assetName1 = assetName1;
            _assetName2 = assetName2;
        }

        public bool IsComplete => _isComplete;

        public void Finish(IFeatureCollection collection)
        {
            var dims = collection.GetFeature<IPathMappingFeature>();
            _assetIndex1 = dims.GetDimension(_assetName1);
            _assetIndex2 = dims.GetDimension(_assetName2);

            _isComplete = true;
        }

        public void Process(IPathBlock block)
        {
            for (var path = 0; path < block.NumberOfPaths; path += Vector<double>.Count)
            {
                var p1 = block.GetStepsForFactor(path, _assetIndex1);
                var p2 = block.GetStepsForFactor(path, _assetIndex2);
                var p1Arr = new double[Vector<double>.Count][];
                var p2Arr = new double[Vector<double>.Count][];
                for (var i = 0; i < Vector<double>.Count; i++)
                {
                    p1Arr[i] = new double[p1.Length];
                    p2Arr[i] = new double[p2.Length];
                }
                for (var step = 0; step < p1.Length; step++)
                {
                    for (var i = 0; i < Vector<double>.Count; i++)
                    {
                        p1Arr[i][step] = p1[step][i];
                        p2Arr[i][step] = p2[step][i];
                    }
                }

                var correls = new double[Vector<double>.Count];
                for (var i = 0; i < correls.Length; i++)
                {
                    var returns1 = p1Arr[i].Returns(true);
                    var returns2 = p2Arr[i].Returns(true);
                    correls[i] = returns1.Correlation(returns2).Correlation;
                }

                _results.Add(new Vector<double>(correls));
            }
        }

        public void SetupFeatures(IFeatureCollection pathProcessFeaturesCollection)
        {

        }

        public double AverageResult => _results.Select(x =>
                {
                    var vec = new double[Vector<double>.Count];
                    x.CopyTo(vec);
                    return vec.Average();
                }).Average();

        public double ResultStdError => _results.SelectMany(x =>
        {
            var vec = new double[Vector<double>.Count];
            x.CopyTo(vec);
            return vec;
        }).StdDev();
    }
}
