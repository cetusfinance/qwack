using ExcelDna.Integration;
using Microsoft.Extensions.Logging;
using Qwack.Excel.Dates;
using Qwack.Excel.Services;
using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Qwack.Serialization;
using System.Diagnostics.CodeAnalysis;
using Qwack.Transport.BasicTypes;
using System.Collections.Generic;
using Qwack.Dates;

namespace Qwack.Excel.Utils
{
    public class ExcelUtils
    {
        private static readonly ILogger _logger = ContainerStores.GlobalContainer.GetService<ILoggerFactory>()?.CreateLogger<BusinessDateFunctions>();

        [ExcelFunction(Description = "Returns current date and time", Category = "QUtils", IsVolatile = true)]
        public static object QUtils_Now()
        {
            return DateTime.Now.ToString("s");
        }

        [ExcelFunction(Description = "Returns unique entries from a range", Category = "QUtils")]
        public static object QUtils_Unique(
            [ExcelArgument(Description = "The excel range to find unique values in")] object[,] DataRange,
            [ExcelArgument(Description = "Optional - Sort vector of results - Asc or Desc")] object SortResults)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var unique = DataRange.Cast<object>().Distinct();

                var sortString = SortResults.OptionalExcel("");

                if (sortString != "")
                {
                    var directionEnum = (SortDirection)Enum.Parse(typeof(SortDirection), sortString);
                    if (directionEnum == SortDirection.Ascending)
                    {
                        var numericPortion = unique.Where(x => !(x is string)).OrderBy(x => x);
                        var stringPortion = unique.Where(x => x is string).OrderBy(x => x);
                        unique = numericPortion.Concat(stringPortion);
                    }
                    else // descending
                    {
                        var numericPortion = unique.Where(x => !(x is string)).OrderByDescending(x => x);
                        var stringPortion = unique.Where(x => x is string).OrderByDescending(x => x);
                        unique = stringPortion.Concat(numericPortion);
                    }

                }

                return unique.ToArray().ReturnExcelRangeVector();
            });
        }


        [ExcelFunction(Description = "Returns combination of multiple ranges", Category = "QUtils")]
        public static object QUtils_Join(
            [ExcelArgument(Description = "The first excel range")] object DataRange1,
            [ExcelArgument(Description = "The second excel range")] object DataRange2,
            [ExcelArgument(Description = "The third excel range")] object DataRange3,
            [ExcelArgument(Description = "The fourth excel range")] object DataRange4)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var o = new List<object>();

                if (!(DataRange1 is ExcelMissing) && (DataRange1 is object[,] d1))
                {
                    o.AddRange(To1DArray(d1).Where(x => !(x is ExcelEmpty)));
                }
                if (!(DataRange2 is ExcelMissing) && (DataRange2 is object[,] d2))
                {
                    o.AddRange(To1DArray(d2).Where(x => !(x is ExcelEmpty)));
                }
                if (!(DataRange3 is ExcelMissing) && (DataRange3 is object[,] d3))
                {
                    o.AddRange(To1DArray(d3).Where(x=>!(x is ExcelEmpty)));
                }
                if (!(DataRange4 is ExcelMissing) && (DataRange4 is object[,] d4))
                {
                    o.AddRange(To1DArray(d4).Where(x => !(x is ExcelEmpty)));
                }

                return o.ToArray().ReturnExcelRangeVector();
            });
        }

        private static object[] To1DArray(object[,] input)
        {
            var size = input.Length;
            var result = new object[size];

            var write = 0;
            for (var i = 0; i <= input.GetUpperBound(0); i++)
            {
                for (var z = 0; z <= input.GetUpperBound(1); z++)
                {
                    result[write++] = input[i, z];
                }
            }

            return result;
        }

        [ExcelFunction(Description = "Returns sorted entries from a range", Category = "QUtils")]
        public static object QUtils_Sort(
            [ExcelArgument(Description = "The excel range to find unique values in")] object[,] DataRange,
            [ExcelArgument(Description = "Sort direction - Asc or Desc")] string Direction)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var unique = DataRange.Cast<object>();

                var directionEnum = (SortDirection)Enum.Parse(typeof(SortDirection), Direction);
                if (directionEnum == SortDirection.Ascending)
                {
                    var numericPortion = unique.Where(x => !(x is string)).OrderBy(x => x);
                    var stringPortion = unique.Where(x => x is string).OrderBy(x => x);
                    unique = numericPortion.Concat(stringPortion);
                }
                else // descending
                {
                    var numericPortion = unique.Where(x => !(x is string)).OrderByDescending(x => x);
                    var stringPortion = unique.Where(x => x is string).OrderByDescending(x => x);
                    unique = stringPortion.Concat(numericPortion);
                }

                return unique.ToArray().ReturnExcelRangeVector();
            });
        }

        [ExcelFunction(Description = "Removes all whitespace from a string", Category = "QUtils")]
        public static object QUtils_RemoveWhitespace(
            [ExcelArgument(Description = "Input text")] string Text,
            [ExcelArgument(Description = "Characters to remove")] object Characters)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var charArr = Characters.OptionalExcel<string>(" ");
                return new string(Text.Where(x => !charArr.Contains(x)).ToArray());
            });
        }

        [ExcelFunction(Description = "Returns values from a range, filtered on another range", Category = "QUtils")]
        public static object QUtils_Filter(
            [ExcelArgument(Description = "The excel range to extract values from (1d)")] object[] DataRange,
            [ExcelArgument(Description = "The excel range to filter on (1d)")] object[] FilterRange,
            [ExcelArgument(Description = "Value to filter on (optional)")] object FilterValue,
            [ExcelArgument(Description = "Filter on exact match? (optional)")] object ExactMatch)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var validIx = new int[0];
                if (FilterValue is ExcelMissing || FilterValue is ExcelEmpty)
                {
                    validIx = FilterRange.Select((i, ix) => (i is ExcelEmpty) || ((i as string) == "") ? -1 : ix).Where(ix => ix >= 0).ToArray();
                }
                else
                {
                    var exact = ExactMatch.OptionalExcel(false);
                    var filter = FilterValue as string;
                    if(exact)
                        validIx = FilterRange.Select((i, ix) => (i is ExcelEmpty) || ((i is string st) && st!= filter) ? -1 : ix).Where(ix => ix >= 0).ToArray();
                    else
                        validIx = FilterRange.Select((i, ix) => (i is ExcelEmpty) || ((i is string st) && !st.Contains(filter)) ? -1 : ix).Where(ix => ix >= 0).ToArray();
                }
                var filtered = DataRange.Where((x, ix) => validIx.Contains(ix)).ToArray();
                return filtered.ReturnExcelRangeVector();
            });
        }

        [ExcelFunction(Description = "Removes all leading/trailing characters from a string", Category = "QUtils")]
        public static object QUtils_Trim(
            [ExcelArgument(Description = "Input text")] string Text,
            [ExcelArgument(Description = "Characters to trim")] object Characters)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                if (Characters is ExcelMissing)
                    return Text.Trim();

                var charArr = Characters as string;
                return Text.Trim(charArr.ToCharArray());
            });
        }

        [ExcelFunction(Description = "Returns values from a range, removing any errors", Category = "QUtils")]
        public static object QUtils_GoodValues(
            [ExcelArgument(Description = "The excel range to extract values from (1d)")] object[] DataRange)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var filtered = DataRange.Where(x => !(x is ExcelEmpty) && !(x is ExcelDna.Integration.ExcelError)).ToArray();
                return filtered.ReturnExcelRangeVector();
            });
        }

        [ExcelFunction(Description = "Casts all values in a range to numbers", Category = "QUtils")]
        public static object QUtils_ToNumbers(
            [ExcelArgument(Description = "The excel range to cast as numbers")] object[] DataRange)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var filtered = DataRange
                .Where(x => !(x is ExcelEmpty) && !(x is ExcelError))
                .Select(x => Convert.ToString(x));
                filtered = filtered.Select(x => x.Trim())
                .Select(x => x.Trim(", ".ToCharArray()))
                .Select(x => x.Replace(" ", ""))
                .Select(x => x.Replace(",", ""));
                var asNumbers = filtered.Select(x => (object)double.Parse(x)).ToArray();
                return asNumbers.ReturnExcelRangeVector();
            });
        }

        [ExcelFunction(Description = "Computes average for a given period", Category = "QUtils")]
        public static object QUtils_PeriodAverage(
            [ExcelArgument(Description = "Date range")] double[] DateRange,
            [ExcelArgument(Description = "Value range")] double[] ValueRange,
            [ExcelArgument(Description = "Average period start (inc)")] DateTime AveragePeriodStart,
            [ExcelArgument(Description = "Average period end (inc)")] object AveragePeriodEnd)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                if (DateRange.Length != ValueRange.Length)
                    return "Date and value ranges must be of same size";

                DateTime avgEnd;
                if (AveragePeriodEnd is ExcelMissing)
                {
                    AveragePeriodStart = new DateTime(AveragePeriodStart.Year, AveragePeriodStart.Month, 1);
                    avgEnd = new DateTime(AveragePeriodStart.Year, AveragePeriodStart.Month, 1).AddMonths(1).AddDays(-1);
                }
                else
                    avgEnd = DateTime.FromOADate((double)AveragePeriodEnd);

                var dates = ExcelHelper.ToDateTimeArray(DateRange);
                var indices = dates.Select((x, ix) => (x>=AveragePeriodStart && x<= avgEnd) ? ix : -1)
                .Where(x => x != -1)
                .ToArray();
                var average = ValueRange.Select((x, ix) => indices.Contains(ix) ? x : -1).Where(x => x != -1).Average();
                return average;
            });
        }

        [ExcludeFromCodeCoverage]
        [ExcelFunction(Description = "Determines whether a file exists", Category = "QUtils")]
        public static object QUtils_FileExists(
            [ExcelArgument(Description = "Filename")] string Filename)
        {
            return ExcelHelper.Execute(_logger, () => System.IO.File.Exists(Filename));
        }

        [ExcludeFromCodeCoverage]
        [ExcelFunction(Description = "Returns timestamp for a given file", Category = "QUtils")]
        public static object QUtils_FileTimeStamp(
            [ExcelArgument(Description = "Filename")] string Filename)
        {
            return ExcelHelper.Execute(_logger, () =>
               System.IO.File.Exists(Filename) ?
                (object)System.IO.File.GetLastWriteTime(Filename) :
                (object)"File does not exist");
        }

        [ExcludeFromCodeCoverage]
        [ExcelFunction(Description = "Returns filename for newest file in a folder", Category = "QUtils")]
        public static object QUtils_LatestFileInFolder(
        [ExcelArgument(Description = "Filename")] string FolderPath,
        [ExcelArgument(Description = "Search pattern, default *")] object SearchPattern)
        {
            return ExcelHelper.Execute(_logger, () => 
            {

                var files = SearchPattern is ExcelEmpty || !(SearchPattern is string sp) ?
                    System.IO.Directory.GetFiles(FolderPath) :
                    System.IO.Directory.GetFiles(FolderPath, sp);

                var latestFile = string.Empty;
                var latestStamp = DateTime.MinValue;
                foreach(var file in files)
                {
                    var lastAccess = System.IO.Directory.GetLastWriteTime(file);
                    if (lastAccess>latestStamp)
                    {
                        latestFile = file;
                        latestStamp = lastAccess;
                    }
                }
                    
                return latestFile;
            });
        }

        [ExcludeFromCodeCoverage]
        [ExcelFunction(Description = "Copies file from one location to another", Category = "QUtils")]
        public static object QUtils_CopyFile(
            [ExcelArgument(Description = "Filename Source")] string Source,
            [ExcelArgument(Description = "Filename Destination")] string Destination,
            [ExcelArgument(Description = "Allow overwriting, default false")] object AllowOverwrite)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var overwrite = AllowOverwrite.OptionalExcel(false);
                System.IO.File.Copy(Source, Destination, overwrite);
                return "Success";
            });
        }

        [ExcludeFromCodeCoverage]
        [ExcelFunction(Description = "Creates a folder", Category = "QUtils")]
        public static object QUtils_CreateFolder(
            [ExcelArgument(Description = "Folder name")] string Folder)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                System.IO.Directory.CreateDirectory(Folder);
                return "Success";
            });
        }

        [ExcludeFromCodeCoverage]
        [ExcelFunction(Description = "Serializes an object to file", Category = "QUtils")]
        public static object QUtils_SerializeObject(
            [ExcelArgument(Description = "Object name")] string ObjectName,
            [ExcelArgument(Description = "Object type, e.g. Qwack.Core.Models.IAssetFxModel, Qwack.Core")] string ObjectType,
            [ExcelArgument(Description = "Filename")] string FileName)
        {
            return ExcelHelper.Execute(_logger, () => 
            {
                var t = Type.GetType(ObjectType);
                var method = typeof(ContainerStores).GetMethod("GetObjectFromCache");
                var generic = method.MakeGenericMethod(t);
                var obj = generic.Invoke(null, new object[] { ObjectName });

                var s = new BinarySerializer();
                s.PrepareObjectGraph(obj);
                var bytes = s.SerializeObjectGraph();
                System.IO.File.WriteAllBytes(FileName, bytes.ToArray());
                return $"Saved to {FileName}";
            });
        }

        [ExcludeFromCodeCoverage]
        [ExcelFunction(Description = "De-Serializes an object to file", Category = "QUtils")]
        public static object QUtils_DeSerializeObject(
            [ExcelArgument(Description = "Output object name")] string ObjectName,
            [ExcelArgument(Description = "Object type, e.g. Qwack.Core.Models.IAssetFxModel, Qwack.Core")] string ObjectType,
            [ExcelArgument(Description = "Filename")] string FileName)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var t = Type.GetType(ObjectType);
                var s = new BinaryDeserializer();
                var bytes = System.IO.File.ReadAllBytes(FileName);
                var obj = s.DeserializeObjectGraph(bytes);

                var method = typeof(ContainerStores).GetMethod("PutObjectToCache");
                var generic = method.MakeGenericMethod(t);
                generic.Invoke(null, new object[] { ObjectName, obj });

                return $"{ObjectName}¬1";
            });
        }

        [ExcelFunction(Description = "Removes all leading/trailing characters from a string", Category = "QUtils")]
        public static object QUtils_Replace(
            [ExcelArgument(Description = "Input text")] string Text,
            [ExcelArgument(Description = "String to replace")] string ToReplace,
            [ExcelArgument(Description = "What to replace with")]string ReplaceWith)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                return Text.Replace(ToReplace, ReplaceWith);
            });
        }

        [ExcelFunction(Description = "Replaces error input with alternative", Category = "QUtils")]
        public static object QUtils_ErrorReplace(
            [ExcelArgument(Description = "Input value")] object Input,
            [ExcelArgument(Description = "Alternative value")] object Alternative) 
            => ExcelHelper.Execute(_logger, () => ((Input is string str) && str.StartsWith("#")) || Input is ExcelError ? Alternative : Input);


        [ExcelFunction(Description = "Turns an excel into a csv string", Category = "QUtils")]
        public static object QUtils_RangeToXSV(
            [ExcelArgument(Description = "Input range")] object[] Input,
            [ExcelArgument(Description = "Seperator, default ','")] object Seperator)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var s = Seperator.OptionalExcel(",");
                return string.Join(s, Input);
            });
        }

        [ExcelFunction(Description = "Splits a string on a given character and returns the nTh instance", Category = "QUtils")]
        public static object QUtils_Split(
            [ExcelArgument(Description = "Input string")] string Input,
            [ExcelArgument(Description = "Seperator character")] string Seperator,
            [ExcelArgument(Description = "nth instance, negative to count from last backwards")] int n)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var s = Input.Split(Seperator.ToCharArray());
                if (System.Math.Abs(n) > s.Length)
                    return "#Too few instances";
                return n > 0 ? s[n - 1] : s[s.Length + n];
            });
        }

        [ExcelFunction(Description = "Sets multi-threading flag for Qwack functions", Category = "QUtils")]
        public static object QUtils_SetThreadingFlag(
              [ExcelArgument(Description = "Threading enabled, true or false")] bool ThreadingEnabled
            )
        {
            Qwack.Utils.Parallel.ParallelUtils.Instance.MultiThreaded = ThreadingEnabled;
            return $"Threading set to {ThreadingEnabled}";
        }

        [ExcelFunction(Description = "Prevents lazy-loading", Category = "QUtils")]
        public static object QUtils_WarmUp()
        {
            var z1 = AtmVolType.AtmForward;
            var z2 = new Qwack.Dates.Calendar();
            var z3 = new Qwack.Futures.FutureSettings(null);
            var z4 = Qwack.Math.Distributions.Gaussian.GKern(0, 0, 1);
            var z5 = new Qwack.Models.Calibrators.NewtonRaphsonMultiCurveSolver();
            var z6 = new Qwack.Options.Calibrators.AssetSmileSolver();
            var z7 = new Qwack.Paths.BlockSet(1000, 1, 1);
            var z8 = new Qwack.Providers.CSV.NYMEXFutureRecord();
            var z9 = new Qwack.Random.Constant.Constant();
            var z10 = new Qwack.Serialization.SkipSerializationAttribute();
            var z11 = Qwack.Storage.ObjectCategory.Asset;
            var z13 = new Qwack.Math.Interpolation.ConstantHazzardInterpolator();
            var z14 = Qwack.Utils.Parallel.ParallelUtils.Instance.MultiThreaded;
            var z15 = new Qwack.Core.Basic.FxPair();
            return "Qwack is warm";
        }

        [ExcludeFromCodeCoverage]
        [ExcelFunction(Description = "Determines whether cell contains a tenor", Category = "QUtils")]
        public static object QUtils_IsTenor(
            [ExcelArgument(Description = "Value")] object Value) => 
            ExcelHelper.Execute(_logger, () => 
            {
                if (Value is double)
                    return false;
                if (Value is string st)
                    return Frequency.TryParse(st, out var throwAway);
                else
                    return false;
            });
    }
}
