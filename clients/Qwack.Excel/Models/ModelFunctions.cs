using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExcelDna.Integration;
using Qwack.Core.Curves;
using Qwack.Core.Basic;
using Qwack.Excel.Services;
using Qwack.Excel.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Qwack.Dates;
using Qwack.Core.Instruments.Asset;
using Qwack.Core.Instruments.Funding;
using Qwack.Core.Instruments;
using Qwack.Core.Models;
using Qwack.Models.Models;
using Qwack.Core.Cubes;
using Qwack.Models.MCModels;
using Qwack.Excel.Instruments;

namespace Qwack.Excel.Curves
{
    public class ModelFunctions
    {
        private static readonly ILogger _logger = ContainerStores.GlobalContainer.GetService<ILoggerFactory>()?.CreateLogger<ModelFunctions>();


        [ExcelFunction(Description = "Creates a monte-carlo settings object", Category = CategoryNames.Models, Name = CategoryNames.Models + "_" + nameof(CreateMcSettings))]
        public static object CreateMcSettings(
           [ExcelArgument(Description = "Settings object name")] string ObjectName,
           [ExcelArgument(Description = "Number of paths")] int NumberOfPaths,
           [ExcelArgument(Description = "Number of timesteps")] int NumberOfTimesteps,
           [ExcelArgument(Description = "Random number generator, e.g. Sobol or MersenneTwister")] object RandomGenerator,
           [ExcelArgument(Description = "Forward exposure dates for PFE etc")] object PFEDates)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var rGen = RandomGenerator.OptionalExcel("MersenneTwister");

                if (!Enum.TryParse(rGen, out RandomGeneratorType randomGenerator))
                {
                    return $"Could not parse random generator name - {rGen}";
                }

                var settings = new McSettings
                {
                    Generator = randomGenerator,
                    NumberOfPaths = NumberOfPaths,
                    NumberOfTimesteps = NumberOfTimesteps,
                    PfeExposureDates = PFEDates is object[,] pd ? pd.ObjectRangeToVector<double>().ToDateTimeArray() : null
                };
                var settingsCache = ContainerStores.GetObjectCache<McSettings>();
                settingsCache.PutObject(ObjectName, new SessionItem<McSettings> { Name = ObjectName, Value = settings });
                return ObjectName + '¬' + settingsCache.GetObject(ObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Returns PV of a portfolio by monte-carlo given an AssetFx model and MC settings", Category = CategoryNames.Models, Name = CategoryNames.Models + "_" + nameof(McPortfolioPV))]
        public static object McPortfolioPV(
          [ExcelArgument(Description = "Result object name")] string ResultObjectName,
          [ExcelArgument(Description = "Portolio object name")] string PortfolioName,
          [ExcelArgument(Description = "Asset-FX model name")] string ModelName,
          [ExcelArgument(Description = "MC settings name")] string SettingsName,
          [ExcelArgument(Description = "Reporting currency (optional)")] object ReportingCcy,
          [ExcelArgument(Description = "Use local vol (default false)")] object UseLocalVol)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var pfolio = InstrumentFunctions.GetPortfolioOrTradeFromCache(PortfolioName);
                var model = ContainerStores.GetObjectCache<IAssetFxModel>()
                    .GetObjectOrThrow(ModelName, $"Could not find model with name {ModelName}");
                var settings = ContainerStores.GetObjectCache<McSettings>()
                    .GetObjectOrThrow(SettingsName, $"Could not find MC settings with name {SettingsName}");
                var useLocalVol = UseLocalVol.OptionalExcel(false);

                Currency ccy = null;
                if (!(ReportingCcy is ExcelMissing))
                {
                    ccy = ContainerStores.CurrencyProvider[ReportingCcy as string];
                }
                IMcModel mc;

                if (useLocalVol)
                    mc = new AssetFxLocalVolMC(model.Value.BuildDate, pfolio, model.Value, settings.Value, ContainerStores.CurrencyProvider);
                else
                    mc = new AssetFxBlackVolMC(model.Value.BuildDate, pfolio, model.Value, settings.Value, ContainerStores.CurrencyProvider);

                var result = mc.PV(ccy);
                var resultCache = ContainerStores.GetObjectCache<ICube>();
                resultCache.PutObject(ResultObjectName, new SessionItem<ICube> { Name = ResultObjectName, Value = result });
                return ResultObjectName + '¬' + resultCache.GetObject(ResultObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Returns PFE of a portfolio by monte-carlo given an AssetFx model and MC settings", Category = CategoryNames.Models, Name = CategoryNames.Models + "_" + nameof(McPortfolioPFE))]
        public static object McPortfolioPFE(
          [ExcelArgument(Description = "Result object name")] string ResultObjectName,
          [ExcelArgument(Description = "Portolio object name")] string PortfolioName,
          [ExcelArgument(Description = "Asset-FX model name")] string ModelName,
          [ExcelArgument(Description = "MC settings name")] string SettingsName,
          [ExcelArgument(Description = "Confidence level, e.g. 0.95")] double ConfidenceLevel)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var pfolio = InstrumentFunctions.GetPortfolioOrTradeFromCache(PortfolioName);
                var model = ContainerStores.GetObjectCache<IAssetFxModel>()
                    .GetObjectOrThrow(ModelName, $"Could not find model with name {ModelName}");
                var settings = ContainerStores.GetObjectCache<McSettings>()
                    .GetObjectOrThrow(SettingsName, $"Could not find MC settings with name {SettingsName}");

                var mc = new AssetFxBlackVolMC(model.Value.BuildDate, pfolio, model.Value, settings.Value, ContainerStores.CurrencyProvider);

                var result = mc.PFE(ConfidenceLevel);
                var resultCache = ContainerStores.GetObjectCache<ICube>();
                resultCache.PutObject(ResultObjectName, new SessionItem<ICube> { Name = ResultObjectName, Value = result });
                return ResultObjectName + '¬' + resultCache.GetObject(ResultObjectName).Version;
            });
        }
    }
}
