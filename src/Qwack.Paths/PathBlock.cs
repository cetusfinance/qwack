using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Qwack.Paths
{
    public class PathBlock : IDisposable
    {
        private readonly int _numberOfPaths;
        private readonly int _factors;
        private readonly int _numberOfSteps;
        private readonly GCHandle _handle;
        private byte[] _backingArray;
        private int _startPathIndex;
        private static readonly int _sizeOfDouble = sizeof(double);
        private static readonly int _minNumberOfPaths = 512 / 8 / _sizeOfDouble;
        private static readonly int _pathStep = Vector<double>.Count;

        public PathBlock(int numberOfPaths, int factors, int numberOfSteps, int startPathIndex)
        {
            _startPathIndex = startPathIndex;
            _numberOfPaths = numberOfPaths;
            _factors = factors;
            _numberOfSteps = numberOfSteps;
            _backingArray = new byte[numberOfPaths * factors * numberOfSteps * _sizeOfDouble];
            _handle = GCHandle.Alloc(_backingArray, GCHandleType.Pinned);
        }

        public int NumberOfPaths => _numberOfPaths;
        public int Factors => _factors;
        public int NumberOfSteps => _numberOfSteps;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetDoubleIndex(int pathNumber, int factor, int step)
        {
            int index = step + (factor * step) + (factor * step * _pathStep)
        }

        public void Dispose()
        {
            if (_handle.IsAllocated)
            {
                _handle.Free();
            }
            GC.SuppressFinalize(this);
        }

        ~PathBlock()
        {
            Dispose();
        }
    }
}
