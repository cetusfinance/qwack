using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Qwack.Serialization.Test
{
    public class DictionarySerializationFacts
    {
        [Fact]
        public void CanSerializeSimpleDictionary()
        {
            var obj = Create();
            var t = obj.IntDictionary.GetType();
            var i = t.GetInterfaces();
            var binSer = new BinarySerializer();
            binSer.PrepareObjectGraph(obj);
            var span = binSer.SerializeObjectGraph();

            var binDeser = new BinaryDeserializer();
            var newObj = (ObjectWithDictionaries)binDeser.DeserializeObjectGraph(span);

            Assert.Equal(obj.StringDictionary, newObj.StringDictionary);
            Assert.Equal(obj.IntDictionary, newObj.IntDictionary);
            Assert.Null(newObj.NullDictionary);
        }

        public static ObjectWithDictionaries Create()
        {
            var obj = new ObjectWithDictionaries()
            {
                StringDictionary = new Dictionary<string, int>() { ["test"] = 1, ["test2"] = 5 },
                IntDictionary = new Dictionary<int, double>() { [1] = 1.543, [2] = 13.542 },
            };
            return obj;
        }
    }

    public class ObjectWithDictionaries
    {
        public Dictionary<string, int> StringDictionary { get; set; }
        public Dictionary<int, double> IntDictionary { get; set; }
        public Dictionary<float, long> NullDictionary { get; set; }
    }
}
