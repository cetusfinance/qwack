using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Qwack.Core.Models;
using Qwack.Paths.Features;
using Qwack.Paths.Features.Rates;

namespace Qwack.Paths
{
    public class PathEngine : IEnumerable<PathBlock>, IDisposable, IEngineFeature
    {
        private List<IPathProcess> _pathProcesses = new List<IPathProcess>();
        private List<object> _pathProcessFeatures = new List<object>();
        private int _numberOfPaths;
        private FeatureCollection _featureCollection = new FeatureCollection();
        private int _dimensions;
        private int _steps;
        private BlockSet _blockset;

        public PathEngine(int numberOfPaths)
        {
            _numberOfPaths = numberOfPaths;
            _featureCollection.AddFeature<IPathMappingFeature>(new PathMappingFeature());
            _featureCollection.AddFeature<ITimeStepsFeature>(new TimeStepsFeature());
            _featureCollection.AddFeature<IRatesFeature>(new RatesCollection());
            _featureCollection.AddFeature<IEngineFeature>(this);
        }

        public BlockSet BlockSet => _blockset;
        public int NumberOfPaths => _numberOfPaths;

        public void RunProcess()
        {
            _blockset = new BlockSet(_numberOfPaths, _dimensions, _steps);

            foreach (var block in _blockset)
            {
                foreach (var process in _pathProcesses)
                {
                    process.Process(block);
                }
            }
        }

        public FeatureCollection Features => _featureCollection;

        public void AddPathProcess(IPathProcess process) => _pathProcesses.Add(process);

        public void SetupFeatures()
        {
            foreach (var pp in _pathProcesses)
            {
                pp.SetupFeatures(_featureCollection);
            }
            _dimensions = _featureCollection.GetFeature<IPathMappingFeature>().NumberOfDimensions;
            _steps = _featureCollection.GetFeature<ITimeStepsFeature>().TimeStepCount;

            var unfinished = new List<IRequiresFinish>();
            _featureCollection.FinishSetup(unfinished);
            foreach(var process in _pathProcesses)
            {
                if (process is IRequiresFinish finishProcess)
                {
                    finishProcess.Finish(_featureCollection);
                    if(!finishProcess.IsComplete)
                    {
                        unfinished.Add(finishProcess);
                    }
                }
            }
            if(unfinished.Count > 0)
            {
                IterateFinishing(unfinished);
            }
        }

        private void IterateFinishing(List<IRequiresFinish> unfinished)
        {
            var numberFinished = 1;
            while(numberFinished > 0)
            {
                numberFinished = 0;
                for(var i = 0; i < unfinished.Count;i++)
                {
                    if(unfinished[i] != null)
                    {
                        unfinished[i].Finish(_featureCollection);
                        if(unfinished[i].IsComplete)
                        {
                            numberFinished++;
                            unfinished[i] = null;
                        }
                    }
                }
                if(unfinished.All(f => f == null))
                {
                    //Completed!!
                    return;
                }
            }
            throw new InvalidOperationException("Cannot make any forward progress iterating the finish phase of setup");
        }

        public IEnumerator<PathBlock> GetEnumerator() => _blockset.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _blockset.GetEnumerator();

        public void Dispose()
        {
            foreach (var block in _blockset)
            {
                block.Dispose();
            }
        }
    }
}
