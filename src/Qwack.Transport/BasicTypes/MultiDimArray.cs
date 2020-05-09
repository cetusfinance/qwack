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

        public MultiDimArray(T[,] squareInput)
        {
            Length0 = squareInput.GetLength(0);
            Length1 = squareInput.GetLength(1);
            BackingArray = new T[Length0 * Length1];
            for (var i = 0; i < Length0; i++)
                for (var j = 0; j < Length1; j++)           
                    BackingArray[i * Length1 + j] = squareInput[i, j];
        }

        public MultiDimArray(T[][] jaggedInput)
        {
            Length0 = jaggedInput.GetLength(0);
            JaggedLengths = jaggedInput.Select(x => x.Length).ToArray();
            BackingArray = new T[Length0 * JaggedLengths.Sum()];
            for (var i = 0; i < Length0; i++)
                for (var j = 0; j < JaggedLengths[i]; j++)
                    BackingArray[i * JaggedLengths[i] + j] = jaggedInput[i][j];
        }
    }
}
