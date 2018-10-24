using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Qwack.Serialization.Test
{
    public class StructSerializationFacts
    {
        [Fact]
        public void SimpleNestedStructsWork()
        {
            var bin = new BinarySerializer();
            var obj = Create();
            bin.PrepareObjectGraph(obj);
            var data = bin.SerializeObjectGraph();

            var deBin = new BinaryDeserializer();
            var newObj = (ClassWithStruct) deBin.DeserializeObjectGraph(data);

            Assert.Equal(obj.Struct.TestInt, newObj.Struct.TestInt);
            Assert.Equal(obj.Struct.TestDouble, newObj.Struct.TestDouble);
        }

        public static ClassWithStruct Create()
        {
            var str = new ClassWithStruct()
            {
                Struct = new BasicStruct() { TestInt = 100, TestDouble = 4.0 },
            };
            return str;
        }
    }
       
    public class ClassWithStruct
    {
        public BasicStruct Struct { get; set; }
    }

    public struct BasicStruct
    {
        public int TestInt;
        private double _testDoulble;

        public double TestDouble { get => _testDoulble; set => _testDoulble = value; }
    }
}
