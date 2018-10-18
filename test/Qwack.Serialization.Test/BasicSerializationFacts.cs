using System;
using Xunit;

namespace Qwack.Serialization.Test
{
    public class BasicSerializationFacts
    {
        [Fact]
        public void TestSimpleObject()
        {
            var serializer = new BinarySerializer();
            var obj = Create();
            serializer.PrepareObjectGraph(obj);
            var result = serializer.SerializeObjectGraph(null);

            var deserializer = new BinaryDeserializer();
            var deserObj = (BasicObject)deserializer.DeserializeObjectGraph(result);

            Assert.Equal(obj.TestIntZero, deserObj.TestIntZero);
            Assert.Equal(obj.TestStringContent, deserObj.TestStringContent);
            Assert.Equal(obj.TestStringNull, deserObj.TestStringNull);
            Assert.Equal(obj.TestIntMax, deserObj.TestIntMax);
            Assert.Equal(obj.TestIntMin, deserObj.TestIntMin);
            Assert.Equal(obj.TestDoubleMax, deserObj.TestDoubleMax);
            Assert.Equal(obj.TestDoubleMin, deserObj.TestDoubleMin);
            Assert.Equal(obj.TestDoubleNan, deserObj.TestDoubleNan);
            Assert.Equal(obj.TestDoubleZero, deserObj.TestDoubleZero);
            Assert.Equal(obj.TestBoolFalse, deserObj.TestBoolFalse);
            Assert.Equal(obj.TestBoolTrue, deserObj.TestBoolTrue);
            Assert.Equal(obj.TestLongMin, deserObj.TestLongMin);
            Assert.Equal(obj.TestLongMax, deserObj.TestLongMax);
        }

        private BasicObject Create() => new BasicObject()
        {
            TestIntZero = 0,
            TestStringContent = "Content",
            TestStringNull = null,
            TestIntMax = int.MaxValue,
            TestIntMin = int.MinValue,
            TestDoubleMax = double.MaxValue,
            TestDoubleNan = double.NaN,
            TestDoubleMin = double.MinValue,
            TestBoolTrue = true,
            TestBoolFalse = false,
            TestDoubleZero = 0.0,
            TestLongMax = long.MaxValue,
            TestLongMin = long.MinValue,
        };
    }

    public class BasicObject
    {
        public string TestStringContent { get; set; }
        public string TestStringNull { get; set; }
        public int TestIntZero { get; set; }
        public int TestIntMax { get; set; }
        public int TestIntMin { get; set; }
        public long TestLongMin { get; set; }
        public long TestLongMax { get; set; }
        public double TestDoubleNan { get; set; }
        public double TestDoubleZero { get; set; }
        public double TestDoubleMax { get; set; }
        public double TestDoubleMin { get; set; }
        public bool TestBoolTrue { get; set; }
        public bool TestBoolFalse { get; set; }
    }
}
