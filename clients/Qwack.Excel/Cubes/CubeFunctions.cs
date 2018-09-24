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
             [ExcelArgument(Description = "Cube name")] string ObjectName,
             [ExcelArgument(Description = "Pad output with whitespace beyond edges of result - defualt false")] object PaddedOutput)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                if (!ContainerStores.GetObjectCache<ICube>().TryGetObject(ObjectName, out var cube))
                    throw new Exception($"Could not find cube {ObjectName}");

                var pad = PaddedOutput.OptionalExcel(false);
           
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
                    }
                    o[r, o.GetLength(1) - 1] = row.Value;

                    r++;
                }

                return pad ? o.ReturnPrettyExcelRangeVector() : o;
            });
        }

        [ExcelFunction(Description = "Displays value of a cube object for given row", Category = CategoryNames.Cubes, Name = CategoryNames.Cubes + "_" + nameof(DisplayCubeValueForRow))]
        public static object DisplayCubeValueForRow(
            [ExcelArgument(Description = "Cube name")] string ObjectName,
            [ExcelArgument(Description = "Row index, zero-based")] int RowIndex)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var cube = ContainerStores.GetObjectCache<ICube>().GetObjectOrThrow(ObjectName, $"Could not find cube {ObjectName}");
                var rows = cube.Value.GetAllRows();

                if(rows.Length<RowIndex)
                    throw new Exception($"Only {rows.Length} rows in cube");

                return rows[RowIndex].Value;
            });
        }

        [ExcelFunction(Description = "Creates a cube object", Category = CategoryNames.Cubes, Name = CategoryNames.Cubes + "_" + nameof(CreateCube))]
        public static object CreateCube(
             [ExcelArgument(Description = "Cube name")] string ObjectName,
             [ExcelArgument(Description = "Header range")] object[] Headers,
             [ExcelArgument(Description = "Metadata range")] object[,] MetaData,
             [ExcelArgument(Description = "Value range")] double[] Data)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                if (Headers.Length != MetaData.GetLength(1))
                    throw new Exception("Headers must match width of metadata range");

                if (Data.Length != MetaData.GetLength(0))
                    throw new Exception("Data vector must match length of metadata range");

                var cube = new ResultCube();
                var dataTypes = Headers.ToDictionary(h => (string)h, h => typeof(string));
                cube.Initialize(dataTypes);

                for (var r = 0; r < MetaData.GetLength(0); r++)
                {
                    var rowMeta = new object[MetaData.GetLength(1)];
                    for (var c = 0; c < MetaData.GetLength(1); c++)
                    {
                        rowMeta[c] = MetaData[r, c];
                    }
                    cube.AddRow(rowMeta, Data[r]);
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
            [ExcelArgument(Description = "Field to aggregate by")] object[] AggregationField,
            [ExcelArgument(Description = "Aggregation details")] string AggregateAction)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var cubeCache = ContainerStores.GetObjectCache<ICube>();
                var inCube = cubeCache.GetObjectOrThrow(InputObjectName, $"Could not find cube {InputObjectName}");

                var aggDeets = (AggregationAction)Enum.Parse(typeof(AggregationAction), AggregateAction);

                var outCube = inCube.Value.Pivot(AggregationField.ObjectRangeToVector<string>(), aggDeets);

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
                var inCube = cubeCache.GetObjectOrThrow(InputObjectName, $"Could not find cube {InputObjectName}");

                var filterDeets = FilterDetails.RangeToDictionary<string, object>();

                var outCube = inCube.Value.Filter(filterDeets);

                cubeCache.PutObject(OutputObjectName, new SessionItem<ICube> { Name = OutputObjectName, Value = outCube });
                return OutputObjectName + '¬' + cubeCache.GetObject(OutputObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Creates a new cube object by sorting another cube object", Category = CategoryNames.Cubes, Name = CategoryNames.Cubes + "_" + nameof(SortCube))]
        public static object SortCube(
           [ExcelArgument(Description = "Output cube name")] string OutputObjectName,
           [ExcelArgument(Description = "Input cube name")] string InputObjectName,
           [ExcelArgument(Description = "Fields to sort on")] object[] SortDetails)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var cubeCache = ContainerStores.GetObjectCache<ICube>();
                var inCube = cubeCache.GetObjectOrThrow(InputObjectName, $"Could not find cube {InputObjectName}");

                var sortDeets = SortDetails.ObjectRangeToVector<string>().ToList();

                var outCube = inCube.Value.Sort(sortDeets);

                cubeCache.PutObject(OutputObjectName, new SessionItem<ICube> { Name = OutputObjectName, Value = outCube });
                return OutputObjectName + '¬' + cubeCache.GetObject(OutputObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Lists distinct values in a specified field for a given cube", Category = CategoryNames.Cubes, Name = CategoryNames.Cubes + "_" + nameof(FieldValues))]
        public static object FieldValues(
            [ExcelArgument(Description = "Input cube name")] string InputObjectName,
            [ExcelArgument(Description = "Field name")] string FieldName)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var cubeCache = ContainerStores.GetObjectCache<ICube>();
                var inCube = cubeCache.GetObjectOrThrow(InputObjectName, $"Could not find cube {InputObjectName}");

                var output = inCube.Value.KeysForField<string>(FieldName);

                return ((object[])output).ReturnExcelRangeVector();
            });
        }

        [ExcelFunction(Description = "Produces a matrix for two given fields from a given cube", Category = CategoryNames.Cubes, Name = CategoryNames.Cubes + "_" + nameof(CubeToMatrix))]
        public static object CubeToMatrix(
            [ExcelArgument(Description = "Input cube name")] string InputObjectName,
            [ExcelArgument(Description = "Field name vertical")] string FieldNameV,
            [ExcelArgument(Description = "Field name horizontal")] string FieldNameH,
            [ExcelArgument(Description = "Sort fields - true or false")] bool SortFields)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var cubeCache = ContainerStores.GetObjectCache<ICube>();
                var inCube = cubeCache.GetObjectOrThrow(InputObjectName, $"Could not find cube {InputObjectName}");

                var output = inCube.Value.ToMatrix(FieldNameV, FieldNameH, SortFields);

                return output.ReturnPrettyExcelRangeVector();
            });
        }

        [ExcelFunction(Description = "Writes contents of cube object to csv", Category = CategoryNames.Cubes, Name = CategoryNames.Cubes + "_" + nameof(CubeToCSV))]
        public static object CubeToCSV(
            [ExcelArgument(Description = "Input cube name")] string InputObjectName,
            [ExcelArgument(Description = "Output filename")] string FileName)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var cubeCache = ContainerStores.GetObjectCache<ICube>();
                var inCube = cubeCache.GetObjectOrThrow(InputObjectName, $"Could not find cube {InputObjectName}");

                inCube.Value.ToCSVFile(FileName);

                return $"Saved to {FileName}";
            });
        }
    }
}
