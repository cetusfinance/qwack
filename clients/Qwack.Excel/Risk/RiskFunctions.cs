using System;
using ExcelDna.Integration;
using Qwack.Core.Basic;
using Qwack.Excel.Services;
using Qwack.Excel.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Qwack.Dates;
using Qwack.Models.Models;
using Qwack.Core.Cubes;
using Qwack.Excel.Instruments;
using Qwack.Models.Risk;
using Qwack.Core.Instruments.Funding;
using System.Linq;
using System.Collections.Generic;

namespace Qwack.Excel.Curves
{
    public class RiskFunctions
    {
        private const bool Parallel = false;
        private static readonly ILogger _logger = ContainerStores.GlobalContainer.GetService<ILoggerFactory>()?.CreateLogger<ModelFunctions>();

        [ExcelFunction(Description = "Returns PV of a portfolio given an AssetFx model", 
            Category = CategoryNames.Risk, Name = CategoryNames.Risk + "_" + nameof(PortfolioPV), IsThreadSafe = Parallel)]
        public static object PortfolioPV(
            [ExcelArgument(Description = "Result object name")] string ResultObjectName,
            [ExcelArgument(Description = "Portolio object name")] string PortfolioName,
            [ExcelArgument(Description = "Asset-FX or MC model name")] string ModelName,
            [ExcelArgument(Description = "Reporting currency")] string ReportingCcy)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var model = InstrumentFunctions.GetModelFromCache(ModelName, PortfolioName);
                var ccy = ContainerStores.CurrencyProvider[ReportingCcy];
                var result = model.PV(ccy);
                return PushCubeToCache(result, ResultObjectName);
            });
        }

        [ExcelFunction(Description = "Returns asset vega of a portfolio given an AssetFx model", 
            Category = CategoryNames.Risk, Name = CategoryNames.Risk + "_" + nameof(PortfolioVega), IsThreadSafe = Parallel)]
        public static object PortfolioVega(
            [ExcelArgument(Description = "Result object name")] string ResultObjectName,
            [ExcelArgument(Description = "Portolio object name")] string PortfolioName,
            [ExcelArgument(Description = "Asset-FX or MC model name")] string ModelName,
            [ExcelArgument(Description = "Reporting currency")] string ReportingCcy,
            [ExcelArgument(Description = "Parallel execution, default true")] object Parallelize)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var model = InstrumentFunctions.GetModelFromCache(ModelName, PortfolioName);
                var ccy = ContainerStores.CurrencyProvider[ReportingCcy];
                var result = model.AssetVega(ccy, Parallelize.OptionalExcel(true));
                return PushCubeToCache(result, ResultObjectName);
            });
        }

        [ExcelFunction(Description = "Returns asset sega/rega of a portfolio given an AssetFx model", 
            Category = CategoryNames.Risk, Name = CategoryNames.Risk + "_" + nameof(PortfolioSegaRega), IsThreadSafe = Parallel)]
        public static object PortfolioSegaRega(
            [ExcelArgument(Description = "Result object name")] string ResultObjectName,
            [ExcelArgument(Description = "Portolio object name")] string PortfolioName,
            [ExcelArgument(Description = "Asset-FX or MC model name")] string ModelName,
            [ExcelArgument(Description = "Reporting currency")] string ReportingCcy)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var model = InstrumentFunctions.GetModelFromCache(ModelName, PortfolioName);
                var ccy = ContainerStores.CurrencyProvider[ReportingCcy];
                var result = model.AssetSegaRega(ccy);
                return PushCubeToCache(result, ResultObjectName);
            });
        }

        [ExcelFunction(Description = "Returns fx vega of a portfolio given an AssetFx model", 
            Category = CategoryNames.Risk, Name = CategoryNames.Risk + "_" + nameof(PortfolioFxVega), IsThreadSafe = Parallel)]
        public static object PortfolioFxVega(
            [ExcelArgument(Description = "Result object name")] string ResultObjectName,
            [ExcelArgument(Description = "Portolio object name")] string PortfolioName,
            [ExcelArgument(Description = "Asset-FX or MC model name")] string ModelName,
            [ExcelArgument(Description = "Reporting currency")] string ReportingCcy)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var model = InstrumentFunctions.GetModelFromCache(ModelName, PortfolioName);
                var ccy = ContainerStores.CurrencyProvider[ReportingCcy];
                var result = model.FxVega(ccy);
                return PushCubeToCache(result, ResultObjectName);
            });
        }

        [ExcelFunction(Description = "Returns asset delta of a portfolio given an AssetFx model", 
            Category = CategoryNames.Risk, Name = CategoryNames.Risk + "_" + nameof(PortfolioDelta), IsThreadSafe = Parallel)]
        public static object PortfolioDelta(
            [ExcelArgument(Description = "Result object name")] string ResultObjectName,
            [ExcelArgument(Description = "Portolio object name")] string PortfolioName,
            [ExcelArgument(Description = "Asset-FX or MC model name")] string ModelName,
            [ExcelArgument(Description = "Compute gamma, default false")] object ComputeGamma,
            [ExcelArgument(Description = "Parallel execution, default true")] object Parallelize)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var model = InstrumentFunctions.GetModelFromCache(ModelName, PortfolioName);
                var result = model.AssetDelta(ComputeGamma.OptionalExcel(false), Parallelize.OptionalExcel(true));
                return PushCubeToCache(result, ResultObjectName);
            });
        }

        [ExcelFunction(Description = "Returns asset delta of a portfolio given an AssetFx model", 
            Category = CategoryNames.Risk, Name = CategoryNames.Risk + "_" + nameof(PortfolioParallelDelta), IsThreadSafe = Parallel)]
        public static object PortfolioParallelDelta(
            [ExcelArgument(Description = "Result object name")] string ResultObjectName,
            [ExcelArgument(Description = "Portolio object name")] string PortfolioName,
            [ExcelArgument(Description = "Asset-FX or MC model name")] string ModelName)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var model = InstrumentFunctions.GetModelFromCache(ModelName, PortfolioName);
                var result = model.AssetParallelDelta(ContainerStores.CurrencyProvider);
                return PushCubeToCache(result, ResultObjectName);
            });
        }

        [ExcelFunction(Description = "Returns fx delta of a portfolio given an AssetFx model", 
            Category = CategoryNames.Risk, Name = CategoryNames.Risk + "_" + nameof(PortfolioFxDelta), IsThreadSafe = Parallel)]
        public static object PortfolioFxDelta(
            [ExcelArgument(Description = "Result object name")] string ResultObjectName,
            [ExcelArgument(Description = "Portolio object name")] string PortfolioName,
            [ExcelArgument(Description = "Asset-FX or MC model name")] string ModelName,
            [ExcelArgument(Description = "Home currency, e.g. ZAR")] string HomeCcy,
            [ExcelArgument(Description = "Compute gamma, default false")] object ComputeGamma,
            [ExcelArgument(Description = "Report delta on opposite side of pair, default true")] object InverseDelta)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var gamma = ComputeGamma.OptionalExcel(false);
                var inverseD = InverseDelta.OptionalExcel(true);
                var model = InstrumentFunctions.GetModelFromCache(ModelName, PortfolioName);
                var ccy = ContainerStores.CurrencyProvider[HomeCcy];
                var result = model.FxDelta(ccy, ContainerStores.CurrencyProvider, gamma, inverseD);
                return PushCubeToCache(result, ResultObjectName);
            });
        }

        [ExcelFunction(Description = "Returns fx delta of a portfolio given an AssetFx model", 
            Category = CategoryNames.Risk, Name = CategoryNames.Risk + "_" + nameof(PortfolioFxDeltaSpecific), IsThreadSafe = Parallel)]
        public static object PortfolioFxDeltaSpecific(
            [ExcelArgument(Description = "Result object name")] string ResultObjectName,
            [ExcelArgument(Description = "Portolio object name")] string PortfolioName,
            [ExcelArgument(Description = "Asset-FX or MC model name")] string ModelName,
            [ExcelArgument(Description = "Pairs to bump")] object[] PairsToBump,
            [ExcelArgument(Description = "Home currency, e.g. ZAR")] string HomeCcy,
            [ExcelArgument(Description = "Compute gamma, default false")] object ComputeGamma)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var gamma = ComputeGamma.OptionalExcel(false);
                var model = InstrumentFunctions.GetModelFromCache(ModelName, PortfolioName);
                var ccy = ContainerStores.CurrencyProvider[HomeCcy];
                var pairsToBumpStr = PairsToBump.ObjectRangeToVector<string>();
                var pairsToBump = pairsToBumpStr.Select(p => p.FxPairFromString(ContainerStores.CurrencyProvider, ContainerStores.CalendarProvider)).ToList();
                var result = model.FxDeltaSpecific(ccy, pairsToBump, ContainerStores.CurrencyProvider, gamma);
                return PushCubeToCache(result, ResultObjectName);
            });
        }

        [ExcelFunction(Description = "Returns theta and charm of a portfolio given an AssetFx model", 
            Category = CategoryNames.Risk, Name = CategoryNames.Risk + "_" + nameof(PortfolioThetaCharm), IsThreadSafe = Parallel)]
        public static object PortfolioThetaCharm(
           [ExcelArgument(Description = "Result object name")] string ResultObjectName,
           [ExcelArgument(Description = "Portolio object name")] string PortfolioName,
           [ExcelArgument(Description = "Asset-FX model name")] string ModelName,
           [ExcelArgument(Description = "Fwd value date, usually T+1")] DateTime FwdValDate,
           [ExcelArgument(Description = "Reporting currency")] string ReportingCcy,
           [ExcelArgument(Description = "List of FxPairs for fx metrics")] object[] PairsToRisk)
        {

            return ExcelHelper.Execute(_logger, () =>
            {
                var pairs = (PairsToRisk == null || !PairsToRisk.Any() || PairsToRisk.First() is ExcelMissing) ? null :
                PairsToRisk.ObjectRangeToVector<string>()
                        .Select(x => x.FxPairFromString(ContainerStores.CurrencyProvider, ContainerStores.CalendarProvider))
                        .ToList();
                return ThetaCharm(ResultObjectName, PortfolioName, ModelName, FwdValDate, ReportingCcy, true, pairs);
            });
        }

        [ExcelFunction(Description = "Returns theta of a portfolio given an AssetFx model", 
            Category = CategoryNames.Risk, Name = CategoryNames.Risk + "_" + nameof(PortfolioTheta), IsThreadSafe = Parallel)]
        public static object PortfolioTheta(
            [ExcelArgument(Description = "Result object name")] string ResultObjectName,
            [ExcelArgument(Description = "Portolio object name")] string PortfolioName,
            [ExcelArgument(Description = "Asset-FX model name")] string ModelName,
            [ExcelArgument(Description = "Fwd value date, usually T+1")] DateTime FwdValDate,
            [ExcelArgument(Description = "Reporting currency")] string ReportingCcy)
        {
            return ExcelHelper.Execute(_logger, () => ThetaCharm(ResultObjectName, PortfolioName, ModelName, FwdValDate, ReportingCcy, false, null));
        }

        private static string ThetaCharm(string ResultObjectName, string PortfolioName, string ModelName,DateTime FwdValDate, string ReportingCcy, bool computeCharm, List<FxPair> fxPairs=null)
        {
            var model = InstrumentFunctions.GetModelFromCache(ModelName, PortfolioName);
            var ccy = ContainerStores.CurrencyProvider[ReportingCcy];
            var result = model.AssetThetaCharm(FwdValDate, ccy, ContainerStores.CurrencyProvider, computeCharm, fxPairs);
            return PushCubeToCache(result, ResultObjectName);
        }


        [ExcelFunction(Description = "Returns interest rate delta cube of a portfolio given an AssetFx model", 
            Category = CategoryNames.Risk, Name = CategoryNames.Risk + "_" + nameof(PortfolioIrDelta), IsThreadSafe = Parallel)]
        public static object PortfolioIrDelta(
            [ExcelArgument(Description = "Result object name")] string ResultObjectName,
            [ExcelArgument(Description = "Portolio object name")] string PortfolioName,
            [ExcelArgument(Description = "Asset-FX model name")] string ModelName,
            [ExcelArgument(Description = "Optional reporting ccy")] string ReportingCurrency,
            [ExcelArgument(Description = "Bump size, default 0.0001")] object BumpSize)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var bump = BumpSize.OptionalExcel(0.0001);
                var repCCy = ContainerStores.CurrencyProvider.TryGetCurrency(ReportingCurrency, out var ccy) ? ccy : null;
                var model = InstrumentFunctions.GetModelFromCache(ModelName, PortfolioName);
                var result = model.AssetIrDelta(repCCy,bump);
                return PushCubeToCache(result, ResultObjectName);
            });
        }

        [ExcelFunction(Description = "Returns interest rate benchmark delta cube of a portfolio given an AssetFx model", 
            Category = CategoryNames.Risk, Name = CategoryNames.Risk + "_" + nameof(PortfolioIrBenchmarkDelta), IsThreadSafe = Parallel)]
        public static object PortfolioIrBenchmarkDelta(
            [ExcelArgument(Description = "Result object name")] string ResultObjectName,
            [ExcelArgument(Description = "Portolio object name")] string PortfolioName,
            [ExcelArgument(Description = "Asset-FX model name")] string ModelName,
            [ExcelArgument(Description = "Funding instrument collection name")] string FICName,
            [ExcelArgument(Description = "Reporting currency")] string ReportingCcy)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var model = InstrumentFunctions.GetModelFromCache(ModelName, PortfolioName);
                var fic = ContainerStores.GetObjectCache<FundingInstrumentCollection>().GetObjectOrThrow(FICName, $"FIC {FICName} not found in cache");
                var ccy = ContainerStores.CurrencyProvider.GetCurrency(ReportingCcy);
                var result = model.BenchmarkRisk(fic.Value, ContainerStores.CurrencyProvider, ContainerStores.CalendarProvider, ccy);
                return PushCubeToCache(result, ResultObjectName);
            });
        }

        [ExcelFunction(Description = "Returns correlation delta of a portfolio given an AssetFx model", 
            Category = CategoryNames.Risk, Name = CategoryNames.Risk + "_" + nameof(PortfolioCorrelationDelta), IsThreadSafe = Parallel)]
        public static object PortfolioCorrelationDelta(
            [ExcelArgument(Description = "Result object name")] string ResultObjectName,
            [ExcelArgument(Description = "Portolio object name")] string PortfolioName,
            [ExcelArgument(Description = "Asset-FX model name")] string ModelName,
            [ExcelArgument(Description = "Reporting currency")] string ReportingCcy,
            [ExcelArgument(Description = "Epsilon bump size, rho' = rho + epsilon * (1-rho)")] double Epsilon)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var model = InstrumentFunctions.GetModelFromCache(ModelName, PortfolioName);
                var ccy = ContainerStores.CurrencyProvider.GetCurrency(ReportingCcy);
                var result = model.CorrelationDelta(ccy, Epsilon);
                return PushCubeToCache(result, ResultObjectName);
            });
        }


        [ExcelFunction(Description = "Returns greeks cube of a portfolio given an AssetFx model", 
            Category = CategoryNames.Risk, Name = CategoryNames.Risk + "_" + nameof(PortfolioGreeks), IsThreadSafe = Parallel)]
        public static object PortfolioGreeks(
            [ExcelArgument(Description = "Result object name")] string ResultObjectName,
            [ExcelArgument(Description = "Portolio object name")] string PortfolioName,
            [ExcelArgument(Description = "Asset-FX model name")] string ModelName,
            [ExcelArgument(Description = "Fwd value date, usually T+1")] DateTime FwdValDate,
            [ExcelArgument(Description = "Reporting currency")] string ReportingCcy)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var model = InstrumentFunctions.GetModelFromCache(ModelName, PortfolioName);

                var ccy = ContainerStores.CurrencyProvider.GetCurrency(ReportingCcy);
                var result = model.AssetGreeks(FwdValDate, ccy, ContainerStores.CurrencyProvider).Result;
                return PushCubeToCache(result, ResultObjectName);
            });
        }


        [ExcelFunction(Description = "Returns risk ladder for a portfolio given an AssetFx model and some bump parameters", 
            Category = CategoryNames.Risk, Name = CategoryNames.Risk + "_" + nameof(PortfolioRiskLadder), IsThreadSafe = Parallel)]
        public static object PortfolioRiskLadder(
           [ExcelArgument(Description = "Result object name")] string ResultObjectName,
           [ExcelArgument(Description = "Portolio object name")] string PortfolioName,
           [ExcelArgument(Description = "Asset-FX model name")] string ModelName,
           [ExcelArgument(Description = "Asset Id to bump")] string AssetId,
           [ExcelArgument(Description = "Bump type, defualt FlatShift")] object BumpType,
           [ExcelArgument(Description = "Number of bumps (returns 2*N+1 values)")] int NScenarios,
           [ExcelArgument(Description = "Bump step size")] double BumpStep,
           [ExcelArgument(Description = "Risk metric to produce for each scenario")] object RiskMetric,
           [ExcelArgument(Description = "Return differential to base case, default True")] object ReturnDiff)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var model = InstrumentFunctions.GetModelFromCache(ModelName, PortfolioName);

                if (!Enum.TryParse(BumpType.OptionalExcel("FlatShift"), out MutationType bType))
                    throw new Exception($"Unknown bump/mutation type {BumpType}");
                if (!Enum.TryParse(RiskMetric.OptionalExcel("AssetCurveDelta"), out RiskMetric metric))
                    throw new Exception($"Unknown risk metric {RiskMetric}");

                var retDiff = ReturnDiff.OptionalExcel(true);
                var isFx = AssetId.Length == 7 && AssetId[3] == '/';
                ICube result;

                if (isFx)
                {
                    var ccy = ContainerStores.CurrencyProvider.GetCurrency(AssetId.Substring(0, 3));
                    var riskLadder = new RiskLadder(ccy, bType, metric, BumpStep, NScenarios, retDiff);
                    result = riskLadder.Generate(model, model.Portfolio);
                }
                else
                {
                    var riskLadder = new RiskLadder(AssetId, bType, metric, BumpStep, NScenarios, retDiff);
                    result = riskLadder.Generate(model, model.Portfolio);
                }

                return PushCubeToCache(result, ResultObjectName);
            });
        }

        [ExcelFunction(Description = "Returns time ladder for a portfolio given an AssetFx model and some bump parameters", 
            Category = CategoryNames.Risk, Name = CategoryNames.Risk + "_" + nameof(PortfolioTimeLadder), IsThreadSafe = Parallel)]
        public static object PortfolioTimeLadder(
            [ExcelArgument(Description = "Result object name")] string ResultObjectName,
            [ExcelArgument(Description = "Portolio object name")] string PortfolioName,
            [ExcelArgument(Description = "Asset-FX model name")] string ModelName,
            [ExcelArgument(Description = "Number of bumps (returns 2*N+1 values)")] int NScenarios,
            [ExcelArgument(Description = "Calendar, default ZAR")] object Calendar,
            [ExcelArgument(Description = "Risk metric to produce for each scenario")] object RiskMetric,
            [ExcelArgument(Description = "Return differential to base case, default True")] object ReturnDiff)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var model = InstrumentFunctions.GetModelFromCache(ModelName, PortfolioName);

                var fCal = Calendar.OptionalExcel("ZAR");
                if (!ContainerStores.SessionContainer.GetService<ICalendarProvider>().Collection.TryGetCalendar(fCal, out var cal))
                {
                    _logger?.LogInformation("Calendar {calendar} not found in cache", fCal);
                    return $"Calendar {fCal} not found in cache";
                }

                if (!Enum.TryParse(RiskMetric.OptionalExcel("AssetCurveDelta"), out RiskMetric metric))
                    throw new Exception($"Unknown risk metric {RiskMetric}");

                var retDiff = ReturnDiff.OptionalExcel(true);

                var riskLadder = new TimeLadder(metric, NScenarios, cal, ContainerStores.CurrencyProvider, retDiff);
                var result = riskLadder.Generate(model, model.Portfolio);
                return PushCubeToCache(result, ResultObjectName);
            });
        }

        [ExcelFunction(Description = "Returns an asset/currency risk matrix for a portfolio given an AssetFx model and some bump parameters", 
            Category = CategoryNames.Risk, Name = CategoryNames.Risk + "_" + nameof(PortfolioRiskMatrix), IsThreadSafe = Parallel)]
        public static object PortfolioRiskMatrix(
            [ExcelArgument(Description = "Result object name")] string ResultObjectName,
            [ExcelArgument(Description = "Portolio object name")] string PortfolioName,
            [ExcelArgument(Description = "Asset-FX model name")] string ModelName,
            [ExcelArgument(Description = "Asset Id to bump")] string AssetId,
            [ExcelArgument(Description = "Currency to bump")] string Currency,
            [ExcelArgument(Description = "Bump type, defualt FlatShift")] object BumpType,
            [ExcelArgument(Description = "Number of bumps (returns 2*N+1 values)")] int NScenarios,
            [ExcelArgument(Description = "Bump step size asset")] double BumpStepAsset,
            [ExcelArgument(Description = "Bump step size fx")] double BumpStepFx,
            [ExcelArgument(Description = "Risk metric to produce for each scenario")] object RiskMetric,
            [ExcelArgument(Description = "Return differential to base case, default True")] object ReturnDiff,
            [ExcelArgument(Description = "List of FxPairs for fx metrics")] object[] PairsToRisk)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var model = InstrumentFunctions.GetModelFromCache(ModelName, PortfolioName);

                if (!Enum.TryParse(BumpType.OptionalExcel("FlatShift"), out MutationType bType))
                    throw new Exception($"Unknown bump/mutation type {BumpType}");
                if (!Enum.TryParse(RiskMetric.OptionalExcel("AssetCurveDelta"), out RiskMetric metric))
                    throw new Exception($"Unknown risk metric {RiskMetric}");
                var retDiff = ReturnDiff.OptionalExcel(true);


                var isFxFx = AssetId.Length == 7 && AssetId[3] == '/' &&
                             Currency.Length == 7 && Currency[3] == '/';

                ICube result = null;
                if (isFxFx)
                {
                    var pair1 = Currency.FxPairFromString(ContainerStores.CurrencyProvider, ContainerStores.CalendarProvider);
                    var pair2 = AssetId.FxPairFromString(ContainerStores.CurrencyProvider, ContainerStores.CalendarProvider);
                    var riskMatrix = new RiskMatrix(pair2, pair1, bType, metric, BumpStepAsset, BumpStepFx, NScenarios, ContainerStores.CurrencyProvider, retDiff);

                    if(PairsToRisk!=null && PairsToRisk.Any())
                    {
                        riskMatrix.FxPairsForDelta = PairsToRisk.ObjectRangeToVector<string>()
                        .Select(x => x.FxPairFromString(ContainerStores.CurrencyProvider, ContainerStores.CalendarProvider))
                        .ToList();
                    }

                    result = riskMatrix.Generate(model, model.Portfolio);
                }
                else
                {
                    var ccy = ContainerStores.CurrencyProvider.GetCurrency(Currency);
                    var riskMatrix = new RiskMatrix(AssetId, ccy, bType, metric, BumpStepAsset, BumpStepFx, NScenarios, ContainerStores.CurrencyProvider, retDiff);

                    if (PairsToRisk != null && PairsToRisk.Any())
                    {
                        riskMatrix.FxPairsForDelta = PairsToRisk.ObjectRangeToVector<string>()
                        .Select(x => x.FxPairFromString(ContainerStores.CurrencyProvider, ContainerStores.CalendarProvider))
                        .ToList();
                    }

                    result = riskMatrix.Generate(model, model.Portfolio);
                }

                return PushCubeToCache(result, ResultObjectName);
            });
        }

        public static string PushCubeToCache(ICube cube, string ResultObjectName) => ExcelHelper.PushToCache(cube, ResultObjectName);
    }
}
