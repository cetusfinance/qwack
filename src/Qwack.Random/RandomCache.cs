using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using Qwack.Core.Models;
using Qwack.Paths;
using Qwack.Paths.Features;
using Qwack.Random.Sobol;

namespace Qwack.Random
{
    public static class RandomCache
    {
        private static Dictionary<string, RandomStorage> _randomStorage = new Dictionary<string, RandomStorage>();

        public static IDisposable RegisterForRandom(string key)
        {
            lock(_randomStorage)
            {
                if(!_randomStorage.TryGetValue(key, out var storage))
                {
                    storage = new RandomStorage(key);
                    _randomStorage[key] = storage;
                }
                storage.IncrementCount();
                return storage;
            }
        }

        private static void DeregisterForRandom(string key)
        {
            lock(_randomStorage)
            {
                _randomStorage.Remove(key);
            }
        }

        private class RandomStorage : IRequiresFinish, IDisposable, IPathProcess
        {
            private readonly string _key;
            private int _counter;
            private BlockSet _blockset;
            private object _lock = new object();
            public bool IsComplete => true;

            public RandomStorage(string key)
            {
                _key = key;
            }

            public void IncrementCount() => Interlocked.Increment(ref _counter);
            
            private static string GetSobolFilename() => Path.Combine(GetRunningDirectory(), "SobolDirectionNumbers.txt");

            private static string GetRunningDirectory()
            {
                var codeBaseUrl = new Uri(Assembly.GetExecutingAssembly().Location);
                var codeBasePath = Uri.UnescapeDataString(codeBaseUrl.AbsolutePath);
                var dirPath = Path.GetDirectoryName(codeBasePath);
                return dirPath;
            }

            public void Finish(IFeatureCollection collection)
            {
                if (_blockset != null) return;
                lock (_lock)
                {
                    if (_blockset != null) return;
                    var numberOfPaths = collection.GetFeature<IEngineFeature>().NumberOfPaths;

                    //Build the cached blocks we need for the full runs
                    _blockset = new BlockSet(numberOfPaths, collection.GetFeature<IPathMappingFeature>().NumberOfDimensions,
                        collection.GetFeature<ITimeStepsFeature>().TimeStepCount, collection.GetFeature<IEngineFeature>().CompactMemoryMode);

                    var directionNumbers = new SobolDirectionNumbers();
                    var pathGen = new SobolPathGenerator(directionNumbers, 1)
                    {
                        UseNormalInverse = true
                    };
                    pathGen.Finish(collection);
                    foreach (var block in _blockset)
                    {
                        pathGen.Process(block);
                    }
                }
            }
            
            public void Dispose()
            {
                var newCount = Interlocked.Decrement(ref _counter);
                if (newCount == 0)
                {
                    DeregisterForRandom(_key);
                }
            }

            public void SetupFeatures(IFeatureCollection pathProcessFeaturesCollection) { }
            public void Process(IPathBlock block)
            {
                var currentBlock = _blockset.GetBlock(block.BlockIndex);
                Array.Copy(currentBlock.RawData, block.RawData, currentBlock.TotalBlockSize);
            }
        }
    }
}
