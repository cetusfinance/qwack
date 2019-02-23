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
           [ExcelArgument(Description = "Enable debug mode")] object DebugMode,
           [ExcelArgument(Description = "Enable average path correction")] object PathCorrection,
           [ExcelArgument(Description = "Enable reduced memory operation")] object CompactMemoryMode,
           [ExcelArgument(Description = "Avoid regresison if possible for BackPricing options")] object AvoidBPRegression,
           [ExcelArgument(Description = "Metric to calculate, default PV")] object Metric,
           [ExcelArgument(Description = "Credit curve for CVA calc")] object CreditCurve,
           [ExcelArgument(Description = "Funding curve for xVA")] object FundingCurve,
           [ExcelArgument(Description = "Base discount curve for xVA")] object BaseDiscountCurve)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                if (!ContainerStores.GlobalContainer.GetRequiredService<ICurrencyProvider>().TryGetCurrency(ReportingCurrency.OptionalExcel("USD"), out var repCcy))
                    return $"Could not find currency {ReportingCurrency} in cache";

                if (!Enum.TryParse(RandomGenerator.OptionalExcel("MersenneTwister"), out RandomGeneratorType randomGenerator))
                    return $"Could not parse random generator name - {RandomGenerator}";

                if (!Enum.TryParse(PortfolioRegressor.OptionalExcel("MultiLinear"), out PFERegressorType regType))
                    return $"Could not parse portfolio regressor type - {PortfolioRegressor}";

                if (!Enum.TryParse(Metric.OptionalExcel("PV"), out BaseMetric metric))
                    return $"Could not parse metric - {Metric}";

                var fCurve = FundingCurve is ExcelMissing || !(FundingCurve is string fStr) ? 
                    null : 
                    ContainerStores.GetObjectCache<IIrCurve>().GetObjectOrThrow(fStr, $"Unable to find IrCurve {FundingCurve}");

                var bCurve = BaseDiscountCurve is ExcelMissing || !(BaseDiscountCurve is string bStr) ?
                    null :
                    ContainerStores.GetObjectCache<IIrCurve>().GetObjectOrThrow(bStr, $"Unable to find IrCurve {BaseDiscountCurve}");

                var cCurve = CreditCurve is ExcelMissing || !(CreditCurve is string cStr) ?
                    null :
                    ContainerStores.GetObjectCache<HazzardCurve>().GetObjectOrThrow(cStr, $"Unable to find hazzard curve {CreditCurve}");


                var settings = new McSettings
                {
                    Generator = randomGenerator,
                    NumberOfPaths = NumberOfPaths,
                    NumberOfTimesteps = NumberOfTimesteps,
                    ExposureDates = PFEDates is object[,] pd ? pd.ObjectRangeToVector<double>().ToDateTimeArray() : null,
                    ReportingCurrency = repCcy,
                    LocalVol = LocalVol,
                    ExpensiveFuturesSimulation = FuturesSim,
                    PfeRegressorType = regType,
                    Parallelize = Parallel,
                    FuturesMappingTable = (FutMappingDict is ExcelMissing) ?
                        new Dictionary<string, string>() :
                        ((object[,])FutMappingDict).RangeToDictionary<string, string>(),
                    DebugMode = DebugMode.OptionalExcel(false),
                    AveragePathCorrection = PathCorrection.OptionalExcel(false),
                    CompactMemoryMode = CompactMemoryMode.OptionalExcel(false),
                    AvoidRegressionForBackPricing = AvoidBPRegression.OptionalExcel(false),
                    Metric = metric,
                    FundingCurve = fCurve?.Value,
                    CreditCurve = cCurve?.Value,
                    BaseDiscountCurve = bCurve?.Value
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
                settings.Value.Metric = BaseMetric.PFE;
                var mc = new AssetFxMCModel(model.Value.BuildDate, pfolio, model.Value, settings.Value, ContainerStores.CurrencyProvider, ContainerStores.FuturesProvider, ContainerStores.CalendarProvider);

                var result = mc.PFE(ConfidenceLevel);
                return RiskFunctions.PushCubeToCache(result, ResultObjectName);
            });
        }

        [ExcelFunction(Description = "Returns CVA of a portfolio by monte-carlo given an AssetFx model and MC settings", Category = CategoryNames.Models, Name = CategoryNames.Models + "_" + nameof(McPortfolioCVA))]
        public static object McPortfolioCVA(
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
                settings.Value.Metric = BaseMetric.CVA;
                var mc = new AssetFxMCModel(model.Value.BuildDate, pfolio, model.Value, settings.Value, ContainerStores.CurrencyProvider, ContainerStores.FuturesProvider, ContainerStores.CalendarProvider);

                var result = mc.CVA();
                return result;
            });
        }

        [ExcelFunction(Description = "Returns FVA of a portfolio by monte-carlo given an AssetFx model and MC settings", Category = CategoryNames.Models, Name = CategoryNames.Models + "_" + nameof(McPortfolioFVA))]
        public static object McPortfolioFVA(
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
                settings.Value.Metric = BaseMetric.FVA;
                var mc = new AssetFxMCModel(model.Value.BuildDate, pfolio, model.Value, settings.Value, ContainerStores.CurrencyProvider, ContainerStores.FuturesProvider, ContainerStores.CalendarProvider);

                var (FBA, FCA) = mc.FVA();
                var o = new object[2, 2]
                {
                    {"FCA",FCA },
                    {"FBA",FBA },
                };
                return o;
            });
        }

        [ExcelFunction(Description = "Returns expected SA-CCR capital profile of a portfolio by monte-carlo given an AssetFx model and MC settings", Category = CategoryNames.Models, Name = CategoryNames.Models + "_" + nameof(McPortfolioExpectedCapital))]
        public static object McPortfolioExpectedCapital(
          [ExcelArgument(Description = "Result object name")] string ResultObjectName,
          [ExcelArgument(Description = "Portolio object name")] string PortfolioName,
          [ExcelArgument(Description = "Asset-FX model name")] string ModelName,
          [ExcelArgument(Description = "MC settings name")] string SettingsName,
          [ExcelArgument(Description = "Counterparty risk weight")] double CounterpartyRiskWeight,
          [ExcelArgument(Description = "Map for assetIds to hedge sets")] object[,] AssetIdToHedgeSetMap)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var pfolio = InstrumentFunctions.GetPortfolioOrTradeFromCache(PortfolioName);
                var model = ContainerStores.GetObjectCache<IAssetFxModel>()
                    .GetObjectOrThrow(ModelName, $"Could not find model with name {ModelName}");
                var settings = ContainerStores.GetObjectCache<McSettings>()
                    .GetObjectOrThrow(SettingsName, $"Could not find MC settings with name {SettingsName}");
                settings.Value.Metric = BaseMetric.ExpectedCapital;
                settings.Value.CounterpartyRiskWeighting = CounterpartyRiskWeight;
                settings.Value.AssetIdToHedgeGroupMap = AssetIdToHedgeSetMap.RangeToDictionary<string, string>();

                var mc = new AssetFxMCModel(model.Value.BuildDate, pfolio, model.Value, settings.Value, ContainerStores.CurrencyProvider, ContainerStores.FuturesProvider, ContainerStores.CalendarProvider);

                var result = mc.ExpectedCapital();
                return RiskFunctions.PushCubeToCache(result, ResultObjectName);
            });
        }

        [ExcelFunction(Description = "Returns KVA of a portfolio by monte-carlo given an AssetFx model and MC settings", Category = CategoryNames.Models, Name = CategoryNames.Models + "_" + nameof(McPortfolioKVA))]
        public static object McPortfolioKVA(
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
                settings.Value.Metric = BaseMetric.KVA;
                var mc = new AssetFxMCModel(model.Value.BuildDate, pfolio, model.Value, settings.Value, ContainerStores.CurrencyProvider, ContainerStores.FuturesProvider, ContainerStores.CalendarProvider);

                var result = mc.KVA();
                return result;
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
