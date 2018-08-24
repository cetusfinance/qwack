using System;
using System.Collections.Generic;
using System.Linq;
using ExcelDna.Integration;
using Qwack.Excel.Services;
using Qwack.Excel.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Qwack.Core.Cubes;

namespace Qwack.Excel.Cubes
{
    public class CubeFunctions
    {
        private static readonly ILogger _logger = ContainerStores.GlobalContainer.GetService<ILoggerFactory>()?.CreateLogger<CubeFunctions>();

        [ExcelFunction(Description = "Displays a cube object", Category = CategoryNames.Cubes, Name = CategoryNames.Cubes + "_" + nameof(DisplayCube))]
        public static object DisplayCube(
             [ExcelArgument(Description = "Cube name")] string ObjectName)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var cube = ContainerStores.GetObjectCache<ICube>().GetObject(ObjectName);

                var rows = cube.Value.GetAllRows();

                var o = new object[rows.Length + 1, cube.Value.DataTypes.Count];

                var c = 0;
                foreach (var t in cube.Value.DataTypes)
                {
                    o[0, c] = t.Key;
                    c++;
                }

                var r = 1;
                foreach (var row in rows)
                {
                    c = 0;
                    foreach (var t in cube.Value.DataTypes)
                    {
                        o[r, c] = row[t.Key];
                        c++;
                    }
                    r++;
                }

                return o;
            });
        }

        [ExcelFunction(Description = "Creates a cube object", Category = CategoryNames.Cubes, Name = CategoryNames.Cubes + "_" + nameof(CreateCube))]
        public static object CreateCube(
             [ExcelArgument(Description = "Cube name")] string ObjectName,
             [ExcelArgument(Description = "Header range")] object[] Headers,
             [ExcelArgument(Description = "Data range")] object[,] Data)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                if (Headers.Length != Data.GetLength(1))
                    throw new Exception("Headers must match width of data range");

                var cube = new ResultCube();
                var dataTypes = Headers
                                .Select(h => new Tuple<string, Type>((string)h, typeof(string)))
                                .ToDictionary(kv => kv.Item1, kv => kv.Item2);
                cube.Initialize(dataTypes);

                for (var r = 0; r < Data.GetLength(0); r++)
                {
                    var rowDict = new Dictionary<string, object>();
                    for (var c = 0; c < Data.GetLength(1); c++)
                    {
                        rowDict.Add((string)Headers[c], Data[r,c]);
                    }
                }
                var cubeCache = ContainerStores.GetObjectCache<ICube>();
                cubeCache.PutObject(ObjectName, new SessionItem<ICube> { Name = ObjectName, Value = cube });
                return ObjectName + '¬' + cubeCache.GetObject(ObjectName).Version;
            });
        }
    }
}
