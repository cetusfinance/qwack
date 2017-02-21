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
            [ExcelArgument(Description = "Optional (bool) - Return a column (rather than row) vector of results")]object ReturnColumnVector,
            [ExcelArgument(Description = "Optional (bool) - Sort vector of results")]object SortResults)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var unique = DataRange.Cast<object>().Distinct();

                bool returnColumnVector = ReturnColumnVector.OptionalExcel(false);
                bool sort = SortResults.OptionalExcel(false);

                if (sort)
                    unique = unique.OrderBy(x => x);

                if (returnColumnVector)
                {
                    var o = new object[unique.Count(), 1];
                    int c = 0;
                    foreach(var val in unique)
                    {
                        o[c,1] = val;
                        c++;
                    }
                    return o;
                }
                else
                    return unique.ToArray();
            });
        }

    }
}
