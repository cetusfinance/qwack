using System;
using System.Collections.Generic;
using System.Linq;
using ExcelDna.Integration;
using Qwack.Excel.Services;
using Qwack.Excel.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Qwack.Core.Cubes;
using Qwack.Excel.Curves;
using System.Security.Policy;

namespace Qwack.Excel.Cubes
{
    public class CubeFunctions
    {
        private const bool Parallel = true;
        private static readonly ILogger _logger = ContainerStores.GlobalContainer.GetService<ILoggerFactory>()?.CreateLogger<CubeFunctions>();

        [ExcelFunction(Description = "Displays a cube object", Category = CategoryNames.Cubes, Name = CategoryNames.Cubes + "_" + nameof(DisplayCube), IsThreadSafe = Parallel)]
        public static object DisplayCube(
             [ExcelArgument(Description = "Cube name")] string ObjectName,
             [ExcelArgument(Description = "Pad output with whitespace beyond edges of result - defualt false")] object PaddedOutput,
             [ExcelArgument(Description = "Hide zero value rows - defualt false")] object HideZeroRows)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                if (!ContainerStores.GetObjectCache<ICube>().TryGetObject(ObjectName, out var cube))
                    throw new Exception($"Could not find cube {ObjectName}");

                var pad = PaddedOutput.OptionalExcel(false);
                var hideZero = HideZeroRows.OptionalExcel(false);

                var rows = cube.Value.GetAllRows();

                var o = new List<object[]>(); //[rows.Length + 1, cube.Value.DataTypes.Count+1];
                var width = cube.Value.DataTypes.Count + 1;
                o.Add(new object[width]);
                var c = 0;
                foreach (var t in cube.Value.DataTypes)
                {
                    o[0][c] = t.Key;
                    c++;
                }
                o[0][o[0].Length - 1] = "Value";

                var r = 1;
                foreach (var row in rows)
                {
                    if (!hideZero || row.Value != 0)
                    {
                        o.Add(new object[width]);
                        for (c = 0; c < cube.Value.DataTypes.Count; c++)
                        {
                            o[r][c] = row.MetaData[c];
                        }
                        o[r][o[r].Length - 1] = row.Value;

                        r++;
                    }
                }

                var oo = new object[o.Count, width];
                for(var i=0;i<oo.GetLength(0);i++)
                    for (var j = 0; j < oo.GetLength(1); j++)
                        oo[i, j] = o[i][j];

                return pad ? oo.ReturnPrettyExcelRangeVector() : oo;
            });
        }

        [ExcelFunction(Description = "Displays value of a cube object for given row", Category = CategoryNames.Cubes, Name = CategoryNames.Cubes + "_" + nameof(DisplayCubeValueForRow), IsThreadSafe = Parallel)]
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

                if (rows.Length == RowIndex && RowIndex == 0)
                    return 0.0;

                return rows[RowIndex].Value;
            });
        }

        [ExcelFunction(Description = "Displays value sum of all rows for a cube object", Category = CategoryNames.Cubes, Name = CategoryNames.Cubes + "_" + nameof(SumAllRows), IsThreadSafe = Parallel)]
        public static object SumAllRows(
            [ExcelArgument(Description = "Cube name")] string ObjectName)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var cube = ContainerStores.GetObjectCache<ICube>().GetObjectOrThrow(ObjectName, $"Could not find cube {ObjectName}");
                return cube.Value.SumOfAllRows ;
            });
        }

        [ExcelFunction(Description = "Creates a cube object", Category = CategoryNames.Cubes, Name = CategoryNames.Cubes + "_" + nameof(CreateCube), IsThreadSafe = Parallel)]
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

        [ExcelFunction(Description = "Creates a new cube object by aggregating another cube object", Category = CategoryNames.Cubes, Name = CategoryNames.Cubes + "_" + nameof(AggregateCube), IsThreadSafe = Parallel)]
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

        [ExcelFunction(Description = "Creates a new cube object by filtering another cube object", Category = CategoryNames.Cubes, Name = CategoryNames.Cubes + "_" + nameof(FilterCube), IsThreadSafe = Parallel)]
        public static object FilterCube(
           [ExcelArgument(Description = "Output cube name")] string OutputObjectName,
           [ExcelArgument(Description = "Input cube name")] string InputObjectName,
           [ExcelArgument(Description = "Filter fields and values")] object[,] FilterDetails,
           [ExcelArgument(Description = "Filter out? default false (i.e. filter in)")] object FilterOut)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var filterOut = FilterOut.OptionalExcel(false);
                var cubeCache = ContainerStores.GetObjectCache<ICube>();
                var inCube = cubeCache.GetObjectOrThrow(InputObjectName, $"Could not find cube {InputObjectName}");

                var filterDeets = FilterDetails.RangeToKvList<string, object>();

                var outCube = inCube.Value.Filter(filterDeets, filterOut);

                cubeCache.PutObject(OutputObjectName, new SessionItem<ICube> { Name = OutputObjectName, Value = outCube });
                return OutputObjectName + '¬' + cubeCache.GetObject(OutputObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Creates a new cube object by filtering another cube object", Category = CategoryNames.Cubes, 
            Name = CategoryNames.Cubes + "_" + nameof(FilterCubeSpecific), IsThreadSafe = Parallel)]
        public static object FilterCubeSpecific(
            [ExcelArgument(Description = "Output cube name")] string OutputObjectName,
            [ExcelArgument(Description = "Input cube name")] string InputObjectName,
            [ExcelArgument(Description = "Filter field")] string FilterField,
            [ExcelArgument(Description = "Filter value")] object FilterValue,
            [ExcelArgument(Description = "Filter out? default false (i.e. filter in)")] object FilterOut)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var filterOut = FilterOut.OptionalExcel(false);
                var cubeCache = ContainerStores.GetObjectCache<ICube>();
                var inCube = cubeCache.GetObjectOrThrow(InputObjectName, $"Could not find cube {InputObjectName}");

                var filterDeets = new Dictionary<string, object>() { { FilterField, FilterValue } };

                var outCube = inCube.Value.Filter(filterDeets, filterOut);

                cubeCache.PutObject(OutputObjectName, new SessionItem<ICube> { Name = OutputObjectName, Value = outCube });
                return OutputObjectName + '¬' + cubeCache.GetObject(OutputObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Creates a new cube object by sorting another cube object", Category = CategoryNames.Cubes, Name = CategoryNames.Cubes + "_" + nameof(SortCube), IsThreadSafe = Parallel)]
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

        [ExcelFunction(Description = "Lists distinct values in a specified field for a given cube", Category = CategoryNames.Cubes, Name = CategoryNames.Cubes + "_" + nameof(FieldValues), IsThreadSafe = Parallel)]
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

        [ExcelFunction(Description = "Produces a matrix for two given fields from a given cube", Category = CategoryNames.Cubes, Name = CategoryNames.Cubes + "_" + nameof(CubeToMatrix), IsThreadSafe = Parallel)]
        public static object CubeToMatrix(
            [ExcelArgument(Description = "Input cube name")] string InputObjectName,
            [ExcelArgument(Description = "Field name vertical")] string FieldNameV,
            [ExcelArgument(Description = "Field name horizontal")] string FieldNameH,
            [ExcelArgument(Description = "Sort fields - true or false")] bool SortFields,
            [ExcelArgument(Description = "Hide zero value rows/cols - defualt false")] object HideZeroRows)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var cubeCache = ContainerStores.GetObjectCache<ICube>();
                var inCube = cubeCache.GetObjectOrThrow(InputObjectName, $"Could not find cube {InputObjectName}");
                var hideZero = HideZeroRows.OptionalExcel(false);

                var output = inCube.Value.ToMatrix(FieldNameV, FieldNameH, SortFields);

                if(hideZero)
                {
                    //first remove columns
                    var columnsToRemove = new List<int>();

                    for(var c=1;c<output.GetLength(1);c++)
                    {
                        var allZero = true;
                        for (var r=1;r<output.GetLength(0);r++)
                        {
                            if (output[r, c] != null && (double)output[r, c] != 0.0)
                            {
                                allZero = false;
                                break;
                            }
                        }

                        if (allZero) //remove column
                            columnsToRemove.Add(c);
                    }


                    if(columnsToRemove.Any())
                    {
                        var o2 = new object[output.GetLength(0), output.GetLength(1) - columnsToRemove.Count];
                        var count = 0;
                        for (var c = 0; c < output.GetLength(1); c++)
                        {
                            if(columnsToRemove.Contains(c))
                            {
                                count++;
                                continue;
                            }
                            for (var r = 0; r < output.GetLength(0); r++)
                            {
                                o2[r, c - count] = output[r, c];
                            }
                        }

                        output = o2;
                    }
                }

                return output.ReturnPrettyExcelRangeVector();
            });
        }

        [ExcelFunction(Description = "Writes contents of cube object to csv", Category = CategoryNames.Cubes, Name = CategoryNames.Cubes + "_" + nameof(CubeToCSV), IsThreadSafe = Parallel)]
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

        [ExcelFunction(Description = "Reads contents of cube object from csv", Category = CategoryNames.Cubes, Name = CategoryNames.Cubes + "_" + nameof(CubeFromCSV), IsThreadSafe = Parallel)]
        public static object CubeFromCSV(
            [ExcelArgument(Description = "Output cube name")] string ObjectName,
            [ExcelArgument(Description = "Input filename")] string FileName,
            [ExcelArgument(Description = "Has header row, default true")] object HasHeaderRow,
            [ExcelArgument(Description = "Has value column, default true")] object HasValueColumn)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var hasHeader = HasHeaderRow.OptionalExcel(true);
                var hasValue = HasValueColumn.OptionalExcel(true);


                var cube = (!hasHeader && !hasValue) ?
                    CubeEx.FromCSVFileRaw(FileName) :
                    CubeEx.FromCSVFile(FileName, hasHeader, hasValue);
                return RiskFunctions.PushCubeToCache(cube, ObjectName);
            });
        }

        [ExcelFunction(Description = "Creates a new cube, adding a bucketed time field", Category = CategoryNames.Cubes, Name = CategoryNames.Cubes + "_" + nameof(BucketTimeAxis), IsThreadSafe = Parallel)]
        public static object BucketTimeAxis(
            [ExcelArgument(Description = "Output cube name")] string OutputObjectName,
            [ExcelArgument(Description = "Input cube name")] string InputObjectName,
            [ExcelArgument(Description = "Field name to bucket on")] string InputTimeFieldName,
            [ExcelArgument(Description = "Output field name")] string OutputBucketedFieldName,
            [ExcelArgument(Description = "Bucket labels and boundaries, date first, label second")] object[,] BucketLabelsAndBoundaries)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var cubeCache = ContainerStores.GetObjectCache<ICube>();
                var inCube = cubeCache.GetObjectOrThrow(InputObjectName, $"Could not find cube {InputObjectName}");

                var bucketBoundaries = BucketLabelsAndBoundaries.RangeToDictionary<DateTime, string>();

                var outCube = inCube.Value.BucketTimeAxis(InputTimeFieldName, OutputBucketedFieldName, bucketBoundaries);

                cubeCache.PutObject(OutputObjectName, new SessionItem<ICube> { Name = OutputObjectName, Value = outCube });
                return OutputObjectName + '¬' + cubeCache.GetObject(OutputObjectName).Version;
            });
        }
    }
}
