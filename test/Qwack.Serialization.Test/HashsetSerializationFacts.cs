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

        [Fact(Skip = "Failing on null handling")]
        public void CanSerializeFullHashSet()
        {
            var obj = CreateFull();
            var binSer = new BinarySerializer();
            binSer.PrepareObjectGraph(obj);
            var span = binSer.SerializeObjectGraph();

            var binDeser = new BinaryDeserializer();
            var newObj = (ObjectWithFullHashSets)binDeser.DeserializeObjectGraph(span);

            Assert.Equal(3, newObj.NestedHashSet.Count);
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

        public static ObjectWithFullHashSets CreateFull()
        {
            var obj = new ObjectWithFullHashSets()
            {
                NestedHashSet = new HashSet<ObjectWithLists>() { ListSerializationFacts.Create(), ListSerializationFacts.Create() }
            };
            return obj;
        }
    }
       
    public class ObjectWithFullHashSets
    {
        public HashSet<ObjectWithLists> NestedHashSet { get; set; }
    }

    public class ObjectWithHashsets
    {
        public HashSet<string> StringHashset { get; set; }
        public HashSet<int> IntHashset { get; set; }
        public HashSet<int> NullHashset { get; set; }
    }
}
