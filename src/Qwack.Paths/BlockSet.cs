using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Utils.Exceptions;

namespace Qwack.Paths
{
    /// <summary>
    /// Contains a number of path blocks that make up an entire group of paths
    /// needed for pricing in memory
    /// </summary>
    public class BlockSet:IEnumerable<PathBlock>, IDisposable
    {
        private static readonly int _numberOfThreads = Environment.ProcessorCount;
        private int _numberOfPaths;
        private int _factors;
        private int _steps;
        private PathBlock[] _blocks;

        public BlockSet(int numberOfPaths, int factors, int steps)
        {
            if (numberOfPaths % PathBlock.MinNumberOfPaths != 0)
            {
                ExceptionHelper.ThrowException(ExceptionType.InvalidDataAlignment, $"paths need to be a multiple of {PathBlock.MinNumberOfPaths}");
            }
            _steps = steps;
            _factors = factors;
            _numberOfPaths = numberOfPaths;

            var pathsPerBlock = numberOfPaths / (_numberOfThreads * 2);
            var numberOfBlocks = numberOfPaths / pathsPerBlock;
            _blocks = new PathBlock[numberOfBlocks];
            for (int i = 0; i < _blocks.Length; i++)
            {
                _blocks[i] = new PathBlock(PathBlock.MinNumberOfPaths, factors, steps, PathBlock.MinNumberOfPaths * i);
            }
        }

        public IEnumerator<PathBlock> GetEnumerator()
        {
            return new PathBlockEnumerator(_blocks);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private class PathBlockEnumerator:IEnumerator<PathBlock>
        {
            private PathBlock[] _blocks;
            private int _currentIndex;

            public PathBlockEnumerator(PathBlock[] blocks)
            {
                _blocks = blocks;
            }

            public PathBlock Current => _currentIndex == _blocks.Length ? null : _blocks[_currentIndex];
            object IEnumerator.Current => Current;

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                _currentIndex = Math.Min(++_currentIndex, _blocks.Length);
                return _currentIndex < _blocks.Length;
            }

            public void Reset()
            {
                _currentIndex = -1;
            }
        }


        public void Dispose()
        {
            for(int i = 0; i < _blocks.Length;i++)
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
