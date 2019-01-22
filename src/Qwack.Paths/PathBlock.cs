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
        private double[] _backingArray;
        private static readonly int _vectorShift = (int)System.Math.Log(Vector<double>.Count, 2);

        private readonly bool _lazyInit;

        public PathBlock(int numberOfPaths, int factors, int numberOfSteps, int globalPathIndex, bool lazyInit=false)
        {
            GlobalPathIndex = globalPathIndex;
            _numberOfPaths = numberOfPaths;
            _numberOfFactors = factors;
            _numberOfSteps = numberOfSteps;
            _lazyInit = lazyInit;

            if (!_lazyInit)
                Init();
        }

        private void CheckInit()
        {
            if (_backingArray == null)
            {
                lock (_threadLock)
                {
                    if (_backingArray == null)
                    {
                        Init();
                    }
                }
            }
        }
        private void Init()
        {
            _backingArray = new double[_numberOfPaths * _numberOfFactors * _numberOfSteps];
            _handle = GCHandle.Alloc(_backingArray, GCHandleType.Pinned);
        }

        public int GlobalPathIndex { get; }
        public int NumberOfPaths => _numberOfPaths;
        public int Factors => _numberOfFactors;
        public int NumberOfSteps => _numberOfSteps;
        public static int MinNumberOfPaths => Vector<double>.Count;
        public int TotalBlockSize => _numberOfPaths * _numberOfFactors * _numberOfSteps;
        public double[] RawData => _backingArray;

        private object _threadLock = new object();

        public double this[int index]
        {
            get
            {
                CheckInit();
                return _backingArray[index];
            }
            set
            {
                CheckInit();
                _backingArray[index] = value;
            }
        }

        public unsafe Span<Vector<double>> GetStepsForFactor(int pathId, int factorId)
        {
            CheckInit();

            var byteOffset = GetIndexOfPathStart(pathId, factorId) << 3;
            var pointer = (void*)IntPtr.Add(_handle.AddrOfPinnedObject(), byteOffset);
            var span = new Span<Vector<double>>(pointer, _numberOfSteps);
            return span;
        }

        public unsafe Span<double> GetStepsForFactorSingle(int pathId, int factorId)
        {
            CheckInit();

            var byteOffset = GetIndexOfPathStart(pathId, factorId) << 3;
            var pointer = (void*)IntPtr.Add(_handle.AddrOfPinnedObject(), byteOffset);
            var span = new Span<double>(pointer, _numberOfSteps);
            return span;
        }

        public Span<double> GetEntirePath(int pathId)
        {
            CheckInit();
            return new Span<double>(RawData, GetIndexOfPathStart(pathId, 0), _numberOfFactors * NumberOfSteps);
        }

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
