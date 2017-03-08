using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Qwack.Math.Matrix
{
    public unsafe class FastMatrixColumnsFirst : IFastMatrix
    {
        private GCHandle _handle;
        private int _rows;
        private int _columns;
        private readonly double* _ptr;

        public FastMatrixColumnsFirst(int rows, int columns)
        {
            _columns = columns;
            _rows = rows;
            var storage = new double[rows*columns];
            _handle = GCHandle.Alloc(storage,GCHandleType.Pinned);
            _ptr = (double*) _handle.AddrOfPinnedObject();
        }

        internal FastMatrixColumnsFirst(double* pointer, int rows, int columns)
        {
            _rows = rows;
            _columns = columns;
            _ptr = pointer;
        }

        public double this[int row, int column] { get => _ptr[GetIndex(row, column)]; set => _ptr[GetIndex(row, column)] = value; }
        public double* Pointer => _ptr;
        public int Rows => _rows;
        public int Columns => _columns;

        public static FastMatrixColumnsFirst FromAnotherMatrix(IFastMatrix matrixToClone)
        {
            return new FastMatrixColumnsFirst(matrixToClone.Pointer, matrixToClone.Rows, matrixToClone.Columns);
        }

        public static FastMatrixColumnsFirst Transpose(FastMatrixRowsFirst matrixToClone)
        {
            return new FastMatrixColumnsFirst(matrixToClone.Pointer, matrixToClone.Columns, matrixToClone.Rows);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetIndex(int row, int column)
        {
            return _columns * row + column;
        }
        
        public void Dispose()
        {
            if(_handle.IsAllocated)
            {
                _handle.Free();
            }
        }
    }
}
