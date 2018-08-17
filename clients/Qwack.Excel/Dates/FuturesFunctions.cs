using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExcelDna.Integration;
using Qwack.Core.Curves;
using Qwack.Excel.Services;
using Qwack.Excel.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Qwack.Math.Interpolation;
using Qwack.Core.Basic;
using Qwack.Dates;
using Qwack.Futures;

namespace Qwack.Excel.Dates
{
    public class FuturesFunctions
    {
        private static readonly ILogger _logger = ContainerStores.GlobalContainer.GetService<ILoggerFactory>()?.CreateLogger<FuturesFunctions>();

        [ExcelFunction(Description = "Returns expiry date for a given futures code", Category = CategoryNames.Dates, Name = CategoryNames.Dates + "_" + nameof(FuturesExpiryFromCode))]
        public static object FuturesExpiryFromCode(
             [ExcelArgument(Description = "Futures code, e.g. CLZ3")] string FuturesCode)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var c = new FutureCode(FuturesCode, 2000, ContainerStores.SessionContainer.GetService<IFutureSettingsProvider>());
              
                return c.GetExpiry();
            });
        }

     
    }
}
