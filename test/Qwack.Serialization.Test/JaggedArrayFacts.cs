using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Qwack.Serialization.Test
{
    public class JaggedArrayFacts
    {
        [ThreadStatic]
        private static Random _random = new Random();

        [Fact]
        public void CanSerializeJaggedArrays()
        {
            var serializer = new BinarySerializer();
            var obj = Create();
            serializer.PrepareObjectGraph(obj);
            var result = serializer.SerializeObjectGraph();

            var deserializer = new BinaryDeserializer();
            var deser = (JaggedArraysObject)deserializer.DeserializeObjectGraph(result);

            Comparer(obj, deser);
        }

        public static JaggedArraysObject Create()
        {
            var obj = new JaggedArraysObject()
            {
                DoubleArray = new double[10][],
            };

            for (var i = 0; i < obj.DoubleArray.Length; i++)
            {
                var arrayLength = _random.Next(1, 256);
                obj.DoubleArray[i] = new double[arrayLength];
                for (var x = 0; x < obj.DoubleArray[i].Length; x++)
                {
                    obj.DoubleArray[i][x] = _random.NextDouble();
                }
            }
            return obj;
        }

        private static void Comparer(JaggedArraysObject expected, JaggedArraysObject actual)
        {
            Assert.Equal(expected.DoubleArray.Length, actual.DoubleArray.Length);
            for(var i = 0; i < expected.DoubleArray.Length;i++)
            {
                Assert.Equal(expected.DoubleArray[i], actual.DoubleArray[i]);
            }
        }

        public class JaggedArraysObject
        {
            public double[][] DoubleArray;
        }
    }
}
