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
using Qwack.Core.Curves;
using Qwack.Models.Risk;
using Qwack.Core.Cubes;

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


        [ExcelFunction(Description = "Computes CVA from an EPE profile", Category = CategoryNames.Capital, Name = CategoryNames.Capital + "_" + nameof(ComputeCVA), IsThreadSafe = true)]
        public static object ComputeCVA(
            [ExcelArgument(Description = "Hazzard curve")] string HazzardCurveName,
            [ExcelArgument(Description = "Origin date")] DateTime OriginDate,
            [ExcelArgument(Description = "Discount curve")] string DiscountCurve,
            [ExcelArgument(Description = "EPE profile, cube or array")] object EPEProfile)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var hz = ContainerStores.GetObjectCache<HazzardCurve>().GetObjectOrThrow(HazzardCurveName, $"Hazzard curve {HazzardCurveName} not found");
                var disc = ContainerStores.GetObjectCache<IIrCurve>().GetObjectOrThrow(DiscountCurve, $"Discount curve {DiscountCurve} not found");
                
                if(EPEProfile is object[,] arr)
                {
                    if (arr.GetLength(1) != 2)
                        throw new Exception("Expected Nx2 array for EPE");

                    var epeDates = new DateTime[arr.GetLength(0)];
                    var epeValues = new double[arr.GetLength(0)];
                    for (var i=0;i<arr.GetLength(0);i++)
                    {
                        epeDates[i] = (DateTime)arr[i, 0];
                        epeValues[i] = (double)arr[i, 1];
                    }
                    return CVACalculator.CVA(OriginDate, epeDates, epeValues, hz.Value, disc.Value);
                }
                else if (EPEProfile is string epeCubeName)
                {
                    var cube = ContainerStores.GetObjectCache<ICube>().GetObjectOrThrow(epeCubeName, $"Cube {epeCubeName} not found");
                    return CVACalculator.CVA(OriginDate, cube.Value, hz.Value, disc.Value);
                }

                throw new Exception("EPE profile must be cube reference or Nx2 array");
            });
        }
    }
}
