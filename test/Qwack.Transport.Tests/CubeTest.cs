using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Qwack.Core.Cubes;
using Qwack.Transport.TransportObjects.Cubes;
using Xunit;

namespace Qwack.Transport.Tests
{
    public class CubeTest
    {
        [Fact]
        public void RoundTrip()
        {
            //one cup of cube...
            var cube = new ResultCube();
            var fields = new Dictionary<string, Type>
            {
                {"A",typeof(string) },
                {"B",typeof(int) },
                {"C",typeof(double) },
            };
            cube.Initialize(fields);

            //a pinch of data
            cube.AddRow(new object[] { "Z", 1, 1.1 }, 6.6);
            cube.AddRow(new object[] { "FF", 2, 1.0 }, 7.7);
            cube.AddRow(new object[] { "QQQ", 3, 1.101 }, 8.8);
            cube.AddRow(new object[] { "ABC", 4, 1.111 }, 9.9);

            //serialize @ 180c for .25ms
            var to = cube.ToTransportObject();
            var ms = new MemoryStream();
            ProtoBuf.Serializer.Serialize(ms, to);
            ms.Seek(0, SeekOrigin.Begin);
            var to2 = ProtoBuf.Serializer.Deserialize<TO_ResultCube>(ms);

            var hyperCube = new ResultCube(to2);

            Assert.Equal(cube.SumOfAllRows, hyperCube.SumOfAllRows);
        }
    }
}
