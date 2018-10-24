using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Qwack.Serialization.Test
{
    public class ListSerializationFacts
    {
        [Fact]
        public void CanSerializeSimpleList()
        {
            var obj = Create();
            var binSer = new BinarySerializer();
            binSer.PrepareObjectGraph(obj);
            var span = binSer.SerializeObjectGraph();

            var binDeser = new BinaryDeserializer();
            var newObj = (ObjectWithLists)binDeser.DeserializeObjectGraph(span);

            Assert.Equal(obj.StringList, newObj.StringList);
            Assert.Equal(obj.IntList, newObj.IntList);
            Assert.Null(newObj.NullList);
        }

        public static ObjectWithLists Create()
        {
            var obj = new ObjectWithLists()
            {
                StringList = new List<string>() { "Value1", "Value2", "Value3" },
                IntList = new List<int>() { 100, 800, -100},
            };
            return obj;
        }
    }
       
    public class ObjectWithLists
    {
        public List<string> StringList { get; set; }
        public List<int> IntList { get; set; }
        public List<int> NullList { get; set; }
    }
}
