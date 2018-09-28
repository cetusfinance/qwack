using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Qwack.Core.Models;

namespace Qwack.Paths
{
    public class PathBlock : IDisposable, IPathBlock
    {
        private readonly int _numberOfPaths;
        private readonly int _numberOfFactors;
        private readonly int _numberOfSteps;
        private GCHandle _handle;
        private readonly double[] _backingArray;
        private static readonly int _vectorShift = (int)System.Math.Log(Vector<double>.Count, 2);

        public PathBlock(int numberOfPaths, int factors, int numberOfSteps, int globalPathIndex)
        {
            GlobalPathIndex = globalPathIndex;
            _numberOfPaths = numberOfPaths;
            _numberOfFactors = factors;
            _numberOfSteps = numberOfSteps;
            _backingArray = new double[numberOfPaths * factors * numberOfSteps];
            _handle = GCHandle.Alloc(_backingArray, GCHandleType.Pinned);
        }

        public int GlobalPathIndex { get; }
        public int NumberOfPaths => _numberOfPaths;
        public int Factors => _numberOfFactors;
        public int NumberOfSteps => _numberOfSteps;
        public static int MinNumberOfPaths => Vector<double>.Count;
        public int TotalBlockSize => _numberOfPaths * _numberOfFactors * _numberOfSteps;
        public double[] RawData => _backingArray;

        public double this[int index] { get => _backingArray[index]; set => _backingArray[index] = value; }

        public unsafe Span<Vector<double>> GetStepsForFactor(int pathId, int factorId)
        {
            var byteOffset = GetIndexOfPathStart(pathId, factorId) << 3;
            var pointer = (void*)IntPtr.Add(_handle.AddrOfPinnedObject(), byteOffset);
            var span = new Span<Vector<double>>(pointer, _numberOfSteps);
            return span;
        }

        public Span<double> GetEntirePath(int pathId) => new Span<double>(RawData,GetIndexOfPathStart(pathId,0), _numberOfFactors * NumberOfSteps);

        public int GetIndexOfPathStart(int pathId, int factorId)
        {
            var factorJumpSize = Vector<double>.Count * _numberOfSteps;
            var pathJumpSize = factorJumpSize * _numberOfFactors;
            var pathIndex = pathId / Vector<double>.Count;

            var pathDelta = pathIndex * pathJumpSize;
            var factorDelta = factorJumpSize * factorId;

            var totalIndex = pathDelta + factorDelta;
            return totalIndex;
        }

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
