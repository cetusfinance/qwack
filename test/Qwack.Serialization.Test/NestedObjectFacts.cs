using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Qwack.Serialization.Test
{
    public class NestedObjectFacts
    {
        [Fact]
        public void OneLayerNestedObject()
        {
            var serializer = new BinarySerializer();
            var obj = Create();
            serializer.PrepareObjectGraph(obj);
            var result = serializer.SerializeObjectGraph(null);

            var deserializer = new BinaryDeserializer();
            var deser = (LinearObject)deserializer.DeserializeObjectGraph(result);

            Assert.Null(deser.BasicArraysNull);
            ArraySerializationFacts.Comparer(obj.BasicArrays, deser.BasicArrays);
        }

        public LinearObject Create() => new LinearObject()
        {
            BasicArrays = ArraySerializationFacts.Create(),
            BasicArraysNull = null,
        };
    }

    public class LinearObject
    {
        public BasicArraysObject BasicArrays { get; set; }
        public BasicArraysObject BasicArraysNull { get; set; }
    }
}
