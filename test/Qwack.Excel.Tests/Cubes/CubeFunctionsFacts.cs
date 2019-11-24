using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Qwack.Dates;
using Qwack.Excel.Cubes;
using Qwack.Providers.Json;

namespace Qwack.Excel.Tests.Cubes
{
    public class CubeFunctionsFacts
    {

        [Fact]
        public void CreateCube_Facts()
        {
            var headers = new[] { "b", "c" };
            var metaData = new object[,] { { 1, 2, 3 } };
            var valueData = new double[] { 1.0 };
            Assert.Equal("Headers must match width of metadata range", CubeFunctions.CreateCube("blah", headers, metaData, valueData));

            metaData = new object[,] { { 1, 2 } };
            valueData = new double[] { 1.0, 1.5 };
            Assert.Equal("Data vector must match length of metadata range", CubeFunctions.CreateCube("blah", headers, metaData, valueData));

            valueData = new double[] { 1.0 };
            Assert.Equal("blah¬0", CubeFunctions.CreateCube("blah", headers, metaData, valueData));
        }

        [Fact]
        public void DisplayCube_Facts()
        {
            Assert.Equal("Could not find cube blahblah", CubeFunctions.DisplayCube("blahblah",false));
            
            var headers = new[] { "b", "c" };
            var metaData = new object[,] { { 1, 2 }, { 3, 4 } };
            var valueData = new double[] { 1.0, 1.5 };

            CubeFunctions.CreateCube("blahblah", headers, metaData, valueData);

            var result = (object[,])CubeFunctions.DisplayCube("blahblah", false);
            Assert.Equal(headers[0], result[0, 0]);
            Assert.Equal(metaData[0,1], result[1, 1]);

            Assert.Equal(valueData[1], CubeFunctions.DisplayCubeValueForRow("blahblah", 1));
        }

    }
}
