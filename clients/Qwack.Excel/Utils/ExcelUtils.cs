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

        [ExcelFunction(Description = "Returns unique entries from a range", Category = "QUtils")]
        public static object QUtils_Unique(
            [ExcelArgument(Description = "The excel range to find unique values in")] object[,] DataRange,
            [ExcelArgument(Description = "Optional - Sort vector of results - Asc or Desc")] object SortResults)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var unique = DataRange.Cast<object>().Distinct();

                string sortString = SortResults.OptionalExcel("");

                if (sortString!="")
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

    }
}
