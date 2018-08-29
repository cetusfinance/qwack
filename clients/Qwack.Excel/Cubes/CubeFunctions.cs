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

                var o = new object[rows.Length + 1, cube.Value.DataTypes.Count+1];

                var c = 0;
                foreach (var t in cube.Value.DataTypes)
                {
                    o[0, c] = t.Key;
                    c++;
                }
                o[0, o.GetLength(1) - 1] = "Value";

                var r = 1;
                foreach (var row in rows)
                {
                    for (c = 0; c < cube.Value.DataTypes.Count; c++)
                    {
                        o[r, c] = row.MetaData[c];
                        c++;
                    }
                    o[r, o.GetLength(1) - 1] = row.Value;

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

        [ExcelFunction(Description = "Creates a new cube object by aggregating another cube object", Category = CategoryNames.Cubes, Name = CategoryNames.Cubes + "_" + nameof(AggregateCube))]
        public static object AggregateCube(
            [ExcelArgument(Description = "Output cube name")] string OutputObjectName,
            [ExcelArgument(Description = "Input cube name")] string InputObjectName,
            [ExcelArgument(Description = "Field to aggregate by")] string AggregationField,
            [ExcelArgument(Description = "Aggregation details")] string AggregateAction)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var cubeCache = ContainerStores.GetObjectCache<ICube>();

                if(!cubeCache.TryGetObject(InputObjectName, out var inCube))
                {
                    throw new Exception($"Could not find input cube {InputObjectName}");
                }

                var aggDeets = (AggregationAction)Enum.Parse(typeof(AggregationAction), AggregateAction);

                var outCube = inCube.Value.Pivot(AggregationField, aggDeets);

                cubeCache.PutObject(OutputObjectName, new SessionItem<ICube> { Name = OutputObjectName, Value = outCube });
                return OutputObjectName + '¬' + cubeCache.GetObject(OutputObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Creates a new cube object by filtering another cube object", Category = CategoryNames.Cubes, Name = CategoryNames.Cubes + "_" + nameof(FilterCube))]
        public static object FilterCube(
           [ExcelArgument(Description = "Output cube name")] string OutputObjectName,
           [ExcelArgument(Description = "Input cube name")] string InputObjectName,
           [ExcelArgument(Description = "Filter fields and values")] object[,] FilterDetails)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var cubeCache = ContainerStores.GetObjectCache<ICube>();

                if (!cubeCache.TryGetObject(InputObjectName, out var inCube))
                {
                    throw new Exception($"Could not find input cube {InputObjectName}");
                }

                var filterDeets = FilterDetails.RangeToDictionary<string, object>();

                var outCube = inCube.Value.Filter(filterDeets);

                cubeCache.PutObject(OutputObjectName, new SessionItem<ICube> { Name = OutputObjectName, Value = outCube });
                return OutputObjectName + '¬' + cubeCache.GetObject(OutputObjectName).Version;
            });
        }
    }
}
