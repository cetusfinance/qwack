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
        private GCHandle _handle;
        private double[] _backingArray;
        private int _startPathIndex;
        private readonly int _stepBlockSize;
        private readonly int _blockSize;
        private static readonly int _sizeOfDouble = sizeof(double);
        private static readonly int _minNumberOfPaths = 512 / 8 / _sizeOfDouble;
        private static readonly int _vectorSize = Vector<double>.Count;
        private static readonly int _vectorShift = (int)System.Math.Log(_vectorSize, 2);
        
        public PathBlock(int numberOfPaths, int factors, int numberOfSteps, int startPathIndex)
        {
            _startPathIndex = startPathIndex;
            _numberOfPaths = numberOfPaths;
            _factors = factors;
            _numberOfSteps = numberOfSteps;
            _stepBlockSize = _vectorSize * _factors;
            _blockSize = _vectorSize * _factors * _numberOfSteps;
            _backingArray = new double[numberOfPaths * factors * numberOfSteps];
            _handle = GCHandle.Alloc(_backingArray, GCHandleType.Pinned);
        }

        public int NumberOfPaths => _numberOfPaths;
        public int Factors => _factors;
        public int NumberOfSteps => _numberOfSteps;
        public static int MinNumberOfPaths => _minNumberOfPaths;
        public IntPtr BackingPointer => _handle.AddrOfPinnedObject();
        public int TotalBlockSize => _numberOfPaths * _factors * _numberOfSteps;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetDoubleIndex(int pathNumber, int factor, int step)
        {
            pathNumber = pathNumber - _startPathIndex;
            var blockDelta = (pathNumber / _vectorSize) * _blockSize;
            var stepDelta = _stepBlockSize * step;
            var factorDelta = _vectorSize * factor;
            var pathDelta = pathNumber % _vectorSize;
            return blockDelta + stepDelta + factorDelta + pathDelta;
        }
               
        public double this[int index]
        {
            get
            {
                return _backingArray[index];
            }
            set
            {
                _backingArray[index] = value;
            }
        }

        public unsafe ref Vector<double> ReadVectorByRef(int index)
        {
            return ref Unsafe.AsRef<Vector<double>>((void*)IntPtr.Add(_handle.AddrOfPinnedObject(), index * 8));
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
