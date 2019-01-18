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


        [ExcelFunction(Description = "Creates a monte-carlo settings object", Category = CategoryNames.Models, Name = CategoryNames.Models + "_" + nameof(CreateMcSettings), IsThreadSafe = true)]
        public static object CreateMcSettings(
           [ExcelArgument(Description = "Settings object name")] string ObjectName,
           [ExcelArgument(Description = "Number of paths")] int NumberOfPaths,
           [ExcelArgument(Description = "Number of timesteps")] int NumberOfTimesteps,
           [ExcelArgument(Description = "Random number generator, e.g. Sobol or MersenneTwister")] object RandomGenerator,
           [ExcelArgument(Description = "Forward exposure dates for PFE etc")] object PFEDates,
           [ExcelArgument(Description = "Portfolio regression method for PFE etc")] object PortfolioRegressor,
           [ExcelArgument(Description = "Reporting currency")] object ReportingCurrency,
           [ExcelArgument(Description = "Use Local vol? (True/False)")] bool LocalVol,
           [ExcelArgument(Description = "Full futures simulation? (True/False)")] bool FuturesSim,
           [ExcelArgument(Description = "Parallel execution? (True/False)")] bool Parallel,
           [ExcelArgument(Description = "Futures mapping dictionary, assetId to futures code")] object FutMappingDict,
           [ExcelArgument(Description = "Enable debug mode")] object DebugMode)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var rGen = RandomGenerator.OptionalExcel("MersenneTwister");
                var reportingCurrency = ReportingCurrency.OptionalExcel("USD");
                var regressor = PortfolioRegressor.OptionalExcel("MultiLinear");

                if (!ContainerStores.GlobalContainer.GetRequiredService<ICurrencyProvider>().TryGetCurrency(reportingCurrency, out var repCcy))
                    return $"Could not find currency {reportingCurrency} in cache";

                if (!Enum.TryParse(rGen, out RandomGeneratorType randomGenerator))
                    return $"Could not parse random generator name - {rGen}";

                if (!Enum.TryParse(regressor, out PFERegressorType regType))
                    return $"Could not parse portfolio regressor type - {regressor}";

                var settings = new McSettings
                {
                    Generator = randomGenerator,
                    NumberOfPaths = NumberOfPaths,
                    NumberOfTimesteps = NumberOfTimesteps,
                    PfeExposureDates = PFEDates is object[,] pd ? pd.ObjectRangeToVector<double>().ToDateTimeArray() : null,
                    ReportingCurrency = repCcy,
                    LocalVol = LocalVol,
                    ExpensiveFuturesSimulation = FuturesSim,
                    PfeRegressorType = regType,
                    Parallelize = Parallel,
                    FuturesMappingTable = (FutMappingDict is ExcelMissing) ? 
                        new Dictionary<string, string>() : 
                        ((object[,])FutMappingDict).RangeToDictionary<string,string>(),
                    DebugMode = DebugMode.OptionalExcel(false)
                };

                return ExcelHelper.PushToCache(settings, ObjectName);
            });
        }

        [ExcelFunction(Description = "Creates a monte-carlo model precursor object", Category = CategoryNames.Models, Name = CategoryNames.Models + "_" + nameof(CreateMcModel), IsThreadSafe = true)]
        public static object CreateMcModel(
           [ExcelArgument(Description = "Output object name")] string ObjectName,
           [ExcelArgument(Description = "Asset-FX vanilla model")]string VanillaModel,
           [ExcelArgument(Description = "MC settings")] string McSettings)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var vanillaModel = ContainerStores.GetObjectFromCache<IAssetFxModel>(VanillaModel);
                var mcSettings = ContainerStores.GetObjectFromCache<McSettings>(McSettings);

                var mcModel = new AssetFXMCModelPercursor
                {
                    AssetFxModel = vanillaModel,
                    CalendarProvider = ContainerStores.CalendarProvider,
                    CcyProvider = ContainerStores.CurrencyProvider,
                    FutProvider = ContainerStores.FuturesProvider,
                    McSettings = mcSettings
                };

                return ExcelHelper.PushToCache(mcModel, ObjectName);
            });
        }


        [ExcelFunction(Description = "Returns PV of a portfolio by monte-carlo given an AssetFx model and MC settings", Category = CategoryNames.Models, Name = CategoryNames.Models + "_" + nameof(McPortfolioPV))]
        public static object McPortfolioPV(
          [ExcelArgument(Description = "Result object name")] string ResultObjectName,
          [ExcelArgument(Description = "Portolio object name")] string PortfolioName,
          [ExcelArgument(Description = "Asset-FX model name")] string ModelName,
          [ExcelArgument(Description = "MC settings name")] string SettingsName,
          [ExcelArgument(Description = "Reporting currency (optional)")] object ReportingCcy)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var pfolio = InstrumentFunctions.GetPortfolioOrTradeFromCache(PortfolioName);
                var model = ContainerStores.GetObjectCache<IAssetFxModel>()
                    .GetObjectOrThrow(ModelName, $"Could not find model with name {ModelName}");
                var settings = ContainerStores.GetObjectCache<McSettings>()
                    .GetObjectOrThrow(SettingsName, $"Could not find MC settings with name {SettingsName}");

                Currency ccy = null;
                if (!(ReportingCcy is ExcelMissing))
                    ccy = ContainerStores.CurrencyProvider.GetCurrency(ReportingCcy as string);

                var mc = new AssetFxMCModel(model.Value.BuildDate, pfolio, model.Value, settings.Value, ContainerStores.CurrencyProvider, ContainerStores.FuturesProvider, ContainerStores.CalendarProvider);

                var result = mc.PV(ccy);
                return RiskFunctions.PushCubeToCache(result, ResultObjectName);
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

                var mc = new AssetFxMCModel(model.Value.BuildDate, pfolio, model.Value, settings.Value, ContainerStores.CurrencyProvider, ContainerStores.FuturesProvider, ContainerStores.CalendarProvider);

                var result = mc.PFE(ConfidenceLevel);
                return RiskFunctions.PushCubeToCache(result, ResultObjectName);
            });
        }

        [ExcelFunction(Description = "Returns PFE of a portfolio by monte-carlo given an AssetFx model and MC settings", Category = CategoryNames.Models, Name = CategoryNames.Models + "_" + nameof(McPortfolioEPE))]
        public static object McPortfolioEPE(
         [ExcelArgument(Description = "Result object name")] string ResultObjectName,
         [ExcelArgument(Description = "Portolio object name")] string PortfolioName,
         [ExcelArgument(Description = "Asset-FX model name")] string ModelName,
         [ExcelArgument(Description = "MC settings name")] string SettingsName)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var pfolio = InstrumentFunctions.GetPortfolioOrTradeFromCache(PortfolioName);
                var model = ContainerStores.GetObjectCache<IAssetFxModel>()
                    .GetObjectOrThrow(ModelName, $"Could not find model with name {ModelName}");
                var settings = ContainerStores.GetObjectCache<McSettings>()
                    .GetObjectOrThrow(SettingsName, $"Could not find MC settings with name {SettingsName}");

                var mc = new AssetFxMCModel(model.Value.BuildDate, pfolio, model.Value, settings.Value, ContainerStores.CurrencyProvider, ContainerStores.FuturesProvider, ContainerStores.CalendarProvider);

                var result = mc.EPE();
                return RiskFunctions.PushCubeToCache(result, ResultObjectName);
            });
        }
    }
}
