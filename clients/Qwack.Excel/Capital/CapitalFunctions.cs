using System;
using System.Collections.Generic;
using System.Linq;
using ExcelDna.Integration;
using Qwack.Excel.Services;
using Qwack.Excel.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Qwack.Core.Instruments;
using Qwack.Excel.Curves;
using Qwack.Core.Models;

namespace Qwack.Excel.Capital
{
    public class CapitalFunctions
    {
        private static readonly ILogger _logger = ContainerStores.GlobalContainer.GetService<ILoggerFactory>()?.CreateLogger<CapitalFunctions>();

        [ExcelFunction(Description = "Computes SA-CCR EAD", Category = CategoryNames.Capital, Name = CategoryNames.Capital + "_" + nameof(ComputeEAD), IsThreadSafe = true)]
        public static object ComputeEAD(
             [ExcelArgument(Description = "Portfolio object")] string PortfolioName,
             [ExcelArgument(Description = "AssetFx model")] string VanillaModel,
             [ExcelArgument(Description = "Reporting currency")] string ReportingCurrency,
             [ExcelArgument(Description = "AssetId to Category map")] object[,] AssetIdToCategoryMap)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var pf = Instruments.InstrumentFunctions.GetPortfolioOrTradeFromCache(PortfolioName);
                var model = ContainerStores.GetObjectCache<IAssetFxModel>().GetObjectOrThrow(VanillaModel, $"Model {VanillaModel} not found");
                var ccy = ContainerStores.CurrencyProvider.GetCurrency(ReportingCurrency);
                var mappingDict = AssetIdToCategoryMap.RangeToDictionary<string, string>();
                return pf.SaCcrEAD(model.Value, ccy, mappingDict);
            });
        }

   
    }
}
