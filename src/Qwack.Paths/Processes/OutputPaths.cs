using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Qwack.Core.Models;
using Qwack.Paths.Features;

namespace Qwack.Paths.Processes
{
    public class OutputPaths : IPathProcess, IRequiresFinish
    {
        private string _fileName;
        private int _factorIndex;

        public OutputPaths(string fileName, int factorIndex)
        {
            _factorIndex = factorIndex;
            _fileName = fileName;
        }

        public bool IsComplete => _isComplete;
        private bool _isComplete;

        public void Finish(IFeatureCollection collection)
        {
            using var f = new System.IO.StreamWriter(_fileName, true);
            var dates = collection.GetFeature<ITimeStepsFeature>();
            foreach(var step in dates.Dates) 
            {
                f.Write($"{step.ToString("dd-MM-yyyy")},");
            }
            f.WriteLine();
            _isComplete = true;
        }

        public void Process(IPathBlock block)
        {
            
            using var f = new System.IO.StreamWriter(_fileName, true);
            for (var path = 0; path < block.NumberOfPaths; path += Vector<double>.Count)
            {
                var steps = block.GetStepsForFactor(path, _factorIndex);
                for (var i = 0; i < Vector<double>.Count; i++)
                {
                    for (var step = 0; step < block.NumberOfSteps; step++)
                    {
                        if (step == 0)
                        {
                            f.Write($"{steps[step][i]}");
                        }
                        else
                        {
                            f.Write($",{steps[step][i]}");
                        }
                    }
                    f.WriteLine();
                }

            }
        }

        public void SetupFeatures(IFeatureCollection pathProcessFeaturesCollection)
        {
           
        }
    }
}
