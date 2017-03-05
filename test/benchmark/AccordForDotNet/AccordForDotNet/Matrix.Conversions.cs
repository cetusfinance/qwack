// Accord Math Library
// The Accord.NET Framework
// http://accord-framework.net
//
// Copyright © César Souza, 2009-2017
// cesarsouza at gmail.com
//
//    This library is free software; you can redistribute it and/or
//    modify it under the terms of the GNU Lesser General Public
//    License as published by the Free Software Foundation; either
//    version 2.1 of the License, or (at your option) any later version.
//
//    This library is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//    Lesser General Public License for more details.
//
//    You should have received a copy of the GNU Lesser General Public
//    License along with this library; if not, write to the Free Software
//    Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
//

namespace Accord.Math
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;

    public static partial class Matrix
    {
        /// <summary>
        ///   Creates a vector containing every index that can be used to
        ///   address a given <paramref name="array"/>, in order.
        /// </summary>
        /// 
        /// <param name="array">The array whose indices will be returned.</param>
        /// <param name="deep">Pass true to retrieve all dimensions of the array,
        ///   even if it contains nested arrays (as in jagged matrices).</param>
        /// <param name="max">Bases computations on the maximum length possible for 
        ///   each dimension (in case the jagged matrices has different lengths).</param>
        /// 
        /// <returns>
        ///   An enumerable object that can be used to iterate over all
        ///   positions of the given <paramref name="array">System.Array</paramref>.
        /// </returns>
        /// 
        /// <example>
        /// <code>
        ///   double[,] a = 
        ///   { 
        ///      { 5.3, 2.3 },
        ///      { 4.2, 9.2 }
        ///   };
        ///   
        ///   foreach (int[] idx in a.GetIndices())
        ///   {
        ///      // Get the current element
        ///      double e = (double)a.GetValue(idx);
        ///   }
        /// </code>
        /// </example>
        /// 
        /// <seealso cref="Accord.Math.Vector.GetIndices{T}(T[])"/>
        /// 
        public static IEnumerable<int[]> GetIndices(this Array array, bool deep = false, bool max = false)
        {
            return Combinatorics.Sequences(array.GetLength(deep, max));
        }

        /// <summary>
        ///   Converts a multidimensional array into a jagged array.
        /// </summary>
        /// 
        public static T[][] ToJagged<T>(this T[,] matrix, bool transpose = false)
        {
            T[][] array;

            if (transpose)
            {
                int cols = matrix.GetLength(1);

                array = new T[cols][];
                for (int i = 0; i < cols; i++)
                    array[i] = matrix.GetColumn(i);
            }
            else
            {
                int rows = matrix.GetLength(0);

                array = new T[rows][];
                for (int i = 0; i < rows; i++)
                    array[i] = matrix.GetRow(i);
            }

            return array;
        }

        /// <summary>
        ///   Converts a jagged-array into a multidimensional array.
        /// </summary>
        /// 
        public static T[,] ToMatrix<T>(this T[][] array, bool transpose = false)
        {
            int rows = array.Length;
            if (rows == 0) return new T[0, rows];
            int cols = array[0].Length;

            T[,] m;

            if (transpose)
            {
                m = new T[cols, rows];
                for (int i = 0; i < rows; i++)
                    for (int j = 0; j < cols; j++)
                        m[j, i] = array[i][j];
            }
            else
            {
                m = new T[rows, cols];
                for (int i = 0; i < rows; i++)
                    for (int j = 0; j < cols; j++)
                        m[i, j] = array[i][j];
            }

            return m;
        }

        /// <summary>
        ///   Converts an object into another type, irrespective of whether
        ///   the conversion can be done at compile time or not. This can be
        ///   used to convert generic types to numeric types during runtime.
        /// </summary>
        /// 
        /// <typeparam name="TOutput">The type of the output.</typeparam>
        /// 
        /// <param name="array">The vector or array to be converted.</param>
        /// 
        public static TOutput To<TOutput>(this Array array)
            where TOutput : class, IList, ICollection, IEnumerable
#if !NET35
, IStructuralComparable, IStructuralEquatable
#endif
        {
            return To(array, typeof(TOutput)) as TOutput;
        }

        /// <summary>
        ///   Converts an object into another type, irrespective of whether
        ///   the conversion can be done at compile time or not. This can be
        ///   used to convert generic types to numeric types during runtime.
        /// </summary>
        /// 
        /// <param name="array">The vector or array to be converted.</param>
        /// <param name="outputType">The type of the output.</param>
        /// 
        public static object To(this Array array, Type outputType)
        {
            Type inputType = array.GetType();

            Type inputElementType = inputType.GetElementType();
            Type outputElementType = outputType.GetElementType();

            Array result;

            if (inputElementType.IsArray && !outputElementType.IsArray)
            {
                // jagged -> multidimensional
                result = Array.CreateInstance(outputElementType, array.GetLength(true));

                foreach (var idx in GetIndices(result))
                {
                    object inputValue = array.GetValue(true, idx);
                    object outputValue = convertValue(outputElementType, inputValue);
                    result.SetValue(outputValue, idx);
                }
            }
            else if (!inputElementType.IsArray && outputElementType.IsArray)
            {
                // multidimensional -> jagged
                result = Array.CreateInstance(outputElementType, array.GetLength(0));

                foreach (var idx in GetIndices(array))
                {
                    object inputValue = array.GetValue(idx);
                    object outputValue = convertValue(outputElementType, inputValue);
                    result.SetValue(outputValue, true, idx);
                }
            }
            else
            {
                // Same nature (jagged or multidimensional) array
                result = Array.CreateInstance(outputElementType, array.GetLength(false));

                foreach (var idx in GetIndices(array))
                {
                    object inputValue = array.GetValue(idx);
                    object outputValue = convertValue(outputElementType, inputValue);
                    result.SetValue(outputValue, idx);
                }
            }

            return result;
        }

        /// <summary>
        ///  Gets the value at the specified position in the multidimensional System.Array.
        ///  The indexes are specified as an array of 32-bit integers.
        /// </summary>
        /// 
        /// <param name="array">A jagged or multidimensional array.</param>
        /// <param name="deep">If set to true, internal arrays in jagged arrays will be followed.</param>
        /// <param name="indices">A one-dimensional array of 32-bit integers that represent the
        ///   indexes specifying the position of the System.Array element to get.</param>
        ///   
        public static object GetValue(this Array array, bool deep, int[] indices)
        {
            if (array.IsVector())
                return array.GetValue(indices);

            if (deep && array.IsJagged())
            {
                Array current = array.GetValue(indices[0]) as Array;
                if (indices.Length == 1)
                    return current;
                int[] last = indices.Get(1, 0);
                return GetValue(current, true, last);
            }
            else
            {
                return array.GetValue(indices);
            }
        }

        /// <summary>
        ///   Converts an array into a multidimensional array.
        /// </summary>
        /// 
        public static T[][] ToJagged<T>(this T[] array, bool asColumnVector = true)
        {
            if (asColumnVector)
            {
                T[][] m = new T[array.Length][];
                for (int i = 0; i < array.Length; i++)
                    m[i] = new[] { array[i] };
                return m;
            }
            else
            {
                return new T[][] { array };
            }
        }

        /// <summary>
        ///   Sets a value to the element at the specified position in the multidimensional
        ///   or jagged System.Array. The indexes are specified as an array of 32-bit integers.
        /// </summary>
        /// 
        /// <param name="array">A jagged or multidimensional array.</param>
        /// <param name="value">The new value for the specified element.</param>
        /// <param name="deep">If set to true, internal arrays in jagged arrays will be followed.</param>
        /// <param name="indices">A one-dimensional array of 32-bit integers that represent
        ///   the indexes specifying the position of the element to set.</param>
        ///   
        public static void SetValue(this Array array, object value, bool deep, int[] indices)
        {
            if (deep && array.IsJagged())
            {
                Array current = array.GetValue(indices[0]) as Array;
                int[] last = indices.Get(1, 0);
                SetValue(current, value, true, last);
            }
            else
            {
                array.SetValue(value, indices);
            }
        }

        private static object convertValue(Type outputElementType, object inputValue)
        {
            object outputValue = null;

            Array inputArray = inputValue as Array;

            if (outputElementType.GetTypeInfo().IsEnum)
            {
                outputValue = Enum.ToObject(outputElementType, (int)System.Convert.ChangeType(inputValue, typeof(int)));
            }
            else if (inputArray != null)
            {
                outputValue = To(inputArray, outputElementType);
            }
            else
            {
                outputValue = System.Convert.ChangeType(inputValue, outputElementType);
            }
            return outputValue;
        }
    }
}
