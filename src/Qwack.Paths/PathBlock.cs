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
        private static readonly int _vectorShift = (int)System.Math.Log(Vector<double>.Count, 2);

        public PathBlock(int numberOfPaths, int factors, int numberOfSteps, int startPathIndex)
        {
            _startPathIndex = startPathIndex;
            _numberOfPaths = numberOfPaths;
            _factors = factors;
            _numberOfSteps = numberOfSteps;
            _stepBlockSize = Vector<double>.Count * _factors;
            _blockSize = Vector<double>.Count * _factors * _numberOfSteps;
            _backingArray = new double[numberOfPaths * factors * numberOfSteps];
            _handle = GCHandle.Alloc(_backingArray, GCHandleType.Pinned);
        }

        public int NumberOfPaths => _numberOfPaths;
        public int Factors => _factors;
        public int NumberOfSteps => _numberOfSteps;
        public static int MinNumberOfPaths => Vector<double>.Count;
        public int TotalBlockSize => _numberOfPaths * _factors * _numberOfSteps;
        public double[] RawData => _backingArray;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetDoubleIndex(int pathNumber, int factor, int step)
        {
            var factorDelta = Vector<double>.Count * factor;
            var stepDelta = _factors * step;
            var pathDelta = _factors * _numberOfSteps * pathNumber / Vector<double>.Count;
            var index = factorDelta + stepDelta + pathDelta;
            if (index >= _backingArray.Length) throw new ArgumentOutOfRangeException();
            return index;
        }

        public double this[int index] { get => _backingArray[index]; set => _backingArray[index] = value; }

        public unsafe ref Vector<double> ReadVectorByRef(int index) => ref Unsafe.AsRef<Vector<double>>((void*)IntPtr.Add(_handle.AddrOfPinnedObject(), index << 3));

        public void Dispose()
        {
            if (_handle.IsAllocated)
            {
                _handle.Free();
            }
            GC.SuppressFinalize(this);
        }

        ~PathBlock() => Dispose();
    }
}
