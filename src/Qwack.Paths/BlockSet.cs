using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Utils.Exceptions;
using static System.Math;

namespace Qwack.Paths
{
    /// <summary>
    /// Contains a number of path blocks that make up an entire group of paths
    /// needed for pricing in memory
    /// </summary>
    public class BlockSet : IEnumerable<PathBlock>, IDisposable
    {
        private static readonly int _numberOfThreads = Environment.ProcessorCount;
        private readonly int _numberOfPaths;
        private readonly int _factors;
        private readonly int _steps;
        private PathBlock[] _blocks;

        public BlockSet(int numberOfPaths, int factors, int steps)
        {
            if (numberOfPaths % PathBlock.MinNumberOfPaths != 0)
            {
                ExceptionHelper.ThrowException(ExceptionType.InvalidDataAlignment, $"Paths need to be a multiple of {PathBlock.MinNumberOfPaths}");
            }
            _steps = steps;
            _factors = factors;
            _numberOfPaths = numberOfPaths;

            var pathsPerBlock = numberOfPaths / (_numberOfThreads * 2);
            if(pathsPerBlock==0)
                ExceptionHelper.ThrowException(ExceptionType.InvalidDataAlignment, $"A minimum of {(_numberOfThreads * 2)} need to be run on this machine");

            var numberOfBlocks = numberOfPaths / pathsPerBlock;
            _blocks = new PathBlock[numberOfBlocks];
            for (var i = 0; i < _blocks.Length; i++)
            {
                _blocks[i] = new PathBlock(pathsPerBlock, factors, steps, i * pathsPerBlock);
            }
        }

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
