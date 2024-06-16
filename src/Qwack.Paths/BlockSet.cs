using System;
using System.Collections;
using System.Collections.Generic;
using Qwack.Utils.Exceptions;
using Qwack.Utils.Parallel;
using static System.Math;

namespace Qwack.Paths
{
    /// <summary>
    /// Contains a number of path blocks that make up an entire group of paths
    /// needed for pricing in memory
    /// </summary>
    public sealed class BlockSet : IEnumerable<PathBlock>, IDisposable
    {
        public int NumberOfBlocks => _numberOfBlocks;

        private static readonly int _numberOfThreads = Max(32, ParallelUtils.HighestPowerOfTwoLessThanOrEqualTo(Environment.ProcessorCount));
        private readonly int _numberOfPaths;
        private readonly int _numberOfBlocks;
        private readonly int _factors;
        private readonly int _steps;
        private readonly int _overrun;
        private PathBlock[] _blocks;

        private readonly bool _compactMode;

        public static int RoundedNumberOfPaths(int numberOfPaths)
        {
            var overrun = numberOfPaths % PathBlock.MinNumberOfPaths;
            return numberOfPaths + overrun;
        }

        public BlockSet(int numberOfPaths, int factors, int steps, bool compactMode = false)
        {
            _compactMode = compactMode;
            numberOfPaths = RoundedNumberOfPaths(numberOfPaths);
            _overrun = numberOfPaths % PathBlock.MinNumberOfPaths;

            _steps = steps;
            _factors = factors;
            _numberOfPaths = numberOfPaths;

            //            var pathsPerBlock = (int)System.Math.Ceiling((double)numberOfPaths / (compactMode ? _numberOfThreads * 16 : _numberOfThreads) / 4.0);
            var pathsPerBlock = numberOfPaths / (compactMode ? _numberOfThreads * 16 : _numberOfThreads);

            if (pathsPerBlock == 0)
                ExceptionHelper.ThrowException(ExceptionType.InvalidDataAlignment, $"A minimum of {(_numberOfThreads * 2)} need to be run on this machine");

            _numberOfBlocks = numberOfPaths / pathsPerBlock;
            _blocks = new PathBlock[_numberOfBlocks];
            for (var i = 0; i < _blocks.Length; i++)
            {
                var pathsThisBlock = (i == _blocks.Length - 1) ? pathsPerBlock - _overrun : pathsPerBlock;
                _blocks[i] = new PathBlock(pathsThisBlock, factors, steps, i * pathsPerBlock, i, compactMode);
            }
        }

        public PathBlock GetBlock(int blockIndex) => _blocks[blockIndex];

        public IEnumerator<PathBlock> GetEnumerator() => new PathBlockEnumerator(_blocks);
        public int Steps => _steps;
        public int Factors => _factors;
        public int NumberOfPaths => _numberOfPaths;

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private class PathBlockEnumerator : IEnumerator<PathBlock>
        {
            private readonly PathBlock[] _blocks;
            private int _currentIndex = -1;

            public PathBlockEnumerator(PathBlock[] blocks) => _blocks = blocks;

            public PathBlock Current => _currentIndex == _blocks.Length ? null : _blocks[_currentIndex];
            object IEnumerator.Current => Current;

            public void Dispose()
            {
                //Nothing needed to dispose
            }

            public bool MoveNext()
            {
                _currentIndex++;
                Min(_currentIndex, _blocks.Length);
                return _currentIndex < _blocks.Length;
            }

            public void Reset() => _currentIndex = -1;
        }


        public void Dispose()
        {
            for (var i = 0; i < _blocks.Length; i++)
            {
                _blocks[i].Dispose();
            }
            _blocks = null;
            GC.SuppressFinalize(this);
        }

        ~BlockSet()
        {
            Dispose();
        }
    }
}
