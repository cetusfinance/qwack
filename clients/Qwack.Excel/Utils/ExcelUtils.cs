using ExcelDna.Integration;
using Microsoft.Extensions.Logging;
using Qwack.Dates;
using Qwack.Excel.Dates;
using Qwack.Excel.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;


namespace Qwack.Excel.Utils
{
    public class ExcelUtils
    {
        private static readonly ILogger _logger = ContainerStores.GlobalContainer.GetService<ILoggerFactory>()?.CreateLogger<BusinessDateFunctions>();

        [ExcelFunction(Description = "Returns current date and time", Category = "QUtils")]
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
                return Text.Where(x => !charArr.Contains(x)).ToString();
            });
        }

        [ExcelFunction(Description = "Returns values from a range, filtered on another range", Category = "QUtils")]
        public static object QUtils_Filter(
            [ExcelArgument(Description = "The excel range to extract values from (1d)")] object[] DataRange,
            [ExcelArgument(Description = "The excel range to filter on (1d)")] object[] FilterRange)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var validIx = FilterRange.Select((i, ix) => (i is ExcelEmpty) || ((i as string) == "") ? -1 : ix).Where(ix => ix >= 0);
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
    }
}
