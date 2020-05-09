using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace Qwack.Transport.BasicTypes
{
    [ProtoContract]
    public class MultiDimArray<T>
    {
        [ProtoMember(1)]
        public int Length0 { get; set; }
        [ProtoMember(2)]
        public int Length1 { get; set; }
        [ProtoMember(3)]
        public int[] JaggedLengths { get; set; }
        [ProtoMember(4)]
        public T[] BackingArray { get; set; }

        public static implicit operator T[,](MultiDimArray<T> m) => AsSquareArray(m);
        public static implicit operator T[][](MultiDimArray<T> m) => AsJaggedArray(m);
        public static implicit operator MultiDimArray<T>(T[,] m) => new MultiDimArray<T>(m);
        public static implicit operator MultiDimArray<T>(T[][] m) => new MultiDimArray<T>(m);

        public MultiDimArray(){}

        public MultiDimArray(T[,] squareInput)
        {
            Length0 = squareInput.GetLength(0);
            Length1 = squareInput.GetLength(1);
            BackingArray = new T[Length0 * Length1];
            for (var i = 0; i < Length0; i++)
                for (var j = 0; j < Length1; j++)           
                    BackingArray[i * Length0 + j] = squareInput[i, j];
        }

        public MultiDimArray(T[][] jaggedInput)
        {
            Length0 = jaggedInput.GetLength(0);
            JaggedLengths = jaggedInput.Select(x => x.Length).ToArray();
            BackingArray = new T[Length0 * JaggedLengths.Sum()];
            for (var i = 0; i < Length0; i++)
            {
                var lengthsSoFar = i == 0 ? 0 : JaggedLengths.Take(i).Sum();
                for (var j = 0; j < JaggedLengths[i]; j++)
                    BackingArray[lengthsSoFar + j] = jaggedInput[i][j];
            }
        }

        public static T[,] AsSquareArray(MultiDimArray<T> m)
        {
            var o = new T[m.Length0, m.Length1];
            for (var i = 0; i < m.Length0; i++)
                for (var j = 0; j < m.Length1; j++)
                    o[i,j]=m.BackingArray[i * m.Length0 + j];
            return o;
        }

        public static T[][] AsJaggedArray(MultiDimArray<T> m)
        {
            var o = new T[m.Length0][];
            for (var i = 0; i < m.Length0; i++)
            {
                o[i] = new T[m.JaggedLengths[i]];
                var lengthsSoFar = i == 0 ? 0 : m.JaggedLengths.Take(i).Sum();
                for (var j = 0; j < m.JaggedLengths[i]; j++)
                    o[i][j] = m.BackingArray[lengthsSoFar + j];
            }
            return o;
        }
    }
}
