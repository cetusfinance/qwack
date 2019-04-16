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
            var result = serializer.SerializeObjectGraph();

            var deserializer = new BinaryDeserializer();
            var deser = (BasicArraysObject)deserializer.DeserializeObjectGraph(result);

            Comparer(obj, deser);
        }

        public static void Comparer(BasicArraysObject expected, BasicArraysObject actual)
        {
            Assert.Equal(expected.BoolNullArray, actual.BoolNullArray);
            Assert.Empty(actual.BoolZeroLengthArray);
            Assert.Equal(expected.ByteArray, actual.ByteArray);
            Assert.Equal(expected.DoubleArray, actual.DoubleArray);
            Assert.Equal(expected.UIntArray, actual.UIntArray);
            Assert.Equal(expected.IntArray, actual.IntArray);
            Assert.Equal(expected.ShortArray, actual.ShortArray);
            Assert.Equal(expected.UShortArray, actual.UShortArray);
            Assert.Equal(expected.FloatArray, actual.FloatArray);
            Assert.Equal(expected.StringArray, actual.StringArray);
        }

        public static BasicArraysObject Create()
        {
            var obj = new BasicArraysObject()
            {
                BoolNullArray = null,
                BoolZeroLengthArray = new bool[0],
                ByteArray = new byte[1024],
                UIntArray = new uint[1024],
                IntArray = new int[1024],
                DoubleArray = new double[1024],
                ShortArray = new short[1024],
                UShortArray = new ushort[1024],
                FloatArray = new float[1024],
                StringArray = new [] {string.Empty, null, "Test1", "Test 2 Test", string.Empty, "Test 3 Test", null},
            };
            _random.NextBytes(obj.ByteArray);
            for(var i = 0; i < obj.UIntArray.Length;i++)
            {
                obj.UIntArray[i] = (uint)_random.Next(0, int.MaxValue);
                obj.IntArray[i] = _random.Next();
                obj.DoubleArray[i] = _random.NextDouble();
                obj.UShortArray[i] = (ushort)_random.Next(ushort.MinValue, ushort.MaxValue);
                obj.ShortArray[i] = (short)_random.Next(short.MinValue, short.MaxValue);
                obj.FloatArray[i] = (float)_random.NextDouble();
            }
            return obj;
        }

        [ThreadStatic]
        private static Random _random = new Random();
    }

    public class BasicArraysObject
    {
        public bool[] BoolNullArray { get; set; }
        public bool[] BoolZeroLengthArray { get; set; }
        public byte[] ByteArray { get; set; }
        public uint[] UIntArray { get; set; }
        public int[] IntArray { get; set; }
        public double[] DoubleArray { get; set; }
        public short[] ShortArray { get; set; }
        public ushort[] UShortArray { get; set; }
        public float[] FloatArray { get; set; }
        public string[] StringArray { get; set; }
    }
}
