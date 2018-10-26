using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Qwack.Serialization.Test
{
    public class HashsetSerializationFacts
    {
        [Fact]
        public void CanSerializeSimpleHashset()
        {
            var obj = Create();
            var binSer = new BinarySerializer();
            binSer.PrepareObjectGraph(obj);
            var span = binSer.SerializeObjectGraph();

            var binDeser = new BinaryDeserializer();
            var newObj = (ObjectWithHashsets)binDeser.DeserializeObjectGraph(span);

            Assert.Equal(obj.StringHashset, newObj.StringHashset);
            Assert.Equal(obj.IntHashset, newObj.IntHashset);
            Assert.Null(newObj.NullHashset);
        }

        public static ObjectWithHashsets Create()
        {
            var obj = new ObjectWithHashsets()
            {
                StringHashset = new HashSet<string>() { "Value1", "Value2", "Value3" },
                IntHashset = new HashSet<int>() { 100, 800, -100},
            };
            return obj;
        }
    }
       
    public class ObjectWithHashsets
    {
        public HashSet<string> StringHashset { get; set; }
        public HashSet<int> IntHashset { get; set; }
        public HashSet<int> NullHashset { get; set; }
    }
}
