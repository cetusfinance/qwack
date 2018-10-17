using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Qwack.Serialization.Test
{
    public class ArraySerializationFacts
    {
        [Fact]
        public void CanSerializeSimpleArrays()
        {
            var serializer = new BinarySerializer();
            var obj = Create();
            serializer.PrepareObjectGraph(obj);
            var result = serializer.SerializeObjectGraph(null);

            var deserializer = new BinaryDeserializer();
            var deser = (BasicArraysObject)deserializer.DeserializeObjectGraph(result);

            Assert.Equal(obj.BoolNullArray, deser.BoolNullArray);
            Assert.Empty(deser.BoolZeroLengthArray);
        }

        public static BasicArraysObject Create() => new BasicArraysObject()
        {
            BoolNullArray = null,
            BoolZeroLengthArray = new bool[0],
        };
    }

    public class BasicArraysObject
    {
        public bool[] BoolNullArray { get; set; }
        public bool[] BoolZeroLengthArray { get; set; }
    }
}
