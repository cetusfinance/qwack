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
using Qwack.Models.Solvers;
using Qwack.Models.Models;

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
            [ExcelArgument(Description = "EPE profile, cube or array")] object EPEProfile,
            [ExcelArgument(Description = "Loss-given-default, e.g. 0.4")] double LGD)
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
                    return XVACalculator.CVA(OriginDate, epeDates, epeValues, hz.Value, disc.Value, LGD);
                }
                else if (EPEProfile is string epeCubeName)
                {
                    var cube = ContainerStores.GetObjectCache<ICube>().GetObjectOrThrow(epeCubeName, $"Cube {epeCubeName} not found");
                    return XVACalculator.CVA(OriginDate, cube.Value, hz.Value, disc.Value, LGD);
                }

                throw new Exception("EPE profile must be cube reference or Nx2 array");
            });
        }

        [ExcelFunction(Description = "Computes approximate CVA for a portfolio", Category = CategoryNames.Capital, Name = CategoryNames.Capital + "_" + nameof(ComputeCVAApprox), IsThreadSafe = true)]
        public static object ComputeCVAApprox(
           [ExcelArgument(Description = "Hazzard curve")] string HazzardCurveName,
           [ExcelArgument(Description = "Origin date")] DateTime OriginDate,
           [ExcelArgument(Description = "Exposure dates")] double[] ExposureDates,
           [ExcelArgument(Description = "Discount curve")] string DiscountCurve,
           [ExcelArgument(Description = "Portfolio")] string Portfolio,
           [ExcelArgument(Description = "Asset-FX Model")] string Model,
           [ExcelArgument(Description = "Loss-given-default, e.g. 0.4")] double LGD,
           [ExcelArgument(Description = "Reporting currency")] string Currency)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var hz = ContainerStores.GetObjectCache<HazzardCurve>().GetObjectOrThrow(HazzardCurveName, $"Hazzard curve {HazzardCurveName} not found");
                var disc = ContainerStores.GetObjectCache<IIrCurve>().GetObjectOrThrow(DiscountCurve, $"Discount curve {DiscountCurve} not found");
                var expDates = ExcelHelper.ToDateTimeArray(ExposureDates);
                var portfolio = Instruments.InstrumentFunctions.GetPortfolioOrTradeFromCache(Portfolio);
                var model = ContainerStores.GetObjectCache<IAssetFxModel>().GetObjectOrThrow(Model, $"Asset-FX model {Model} not found");
                var repCcy = ContainerStores.CurrencyProvider.GetCurrency(Currency);
                return XVACalculator.CVA_Approx(expDates, portfolio, hz.Value, model.Value, disc.Value, LGD, repCcy, ContainerStores.CurrencyProvider);
            });
        }

        [ExcelFunction(Description = "Solves strike for a target RoC", Category = CategoryNames.Capital, Name = CategoryNames.Capital + "_" + nameof(SolveStrikeForTargetRoC), IsThreadSafe = true)]
        public static object SolveStrikeForTargetRoC(
          [ExcelArgument(Description = "Hazzard curve")] string HazzardCurveName,
          [ExcelArgument(Description = "Discount curve")] string DiscountCurve,
          [ExcelArgument(Description = "Portfolio")] string Portfolio,
          [ExcelArgument(Description = "Asset-FX Model")] string Model,
          [ExcelArgument(Description = "Loss-given-default, e.g. 0.4")] double LGD,
          [ExcelArgument(Description = "Party risk weight, e.g. 1.0")] double PartyRiskWeight,
          [ExcelArgument(Description = "Reporting currency")] string Currency,
          [ExcelArgument(Description = "Target RoC")] double TargetRoC,
          [ExcelArgument(Description = "Weight for CVA Capital")] double CVACapitalWeight,
          [ExcelArgument(Description = "(Optional) LGD for xVA")] object LGDOverrideXVA)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var hz = ContainerStores.GetObjectCache<HazzardCurve>().GetObjectOrThrow(HazzardCurveName, $"Hazzard curve {HazzardCurveName} not found");
                var disc = ContainerStores.GetObjectCache<IIrCurve>().GetObjectOrThrow(DiscountCurve, $"Discount curve {DiscountCurve} not found");
                var portfolio = Instruments.InstrumentFunctions.GetPortfolioOrTradeFromCache(Portfolio);
                var model = ContainerStores.GetObjectCache<IAssetFxModel>().GetObjectOrThrow(Model, $"Asset-FX model {Model} not found");
                var repCcy = ContainerStores.CurrencyProvider.GetCurrency(Currency);
                var xvaLgd = LGDOverrideXVA.OptionalExcel(LGD);
                return SimplePortfolioSolver.SolveStrikeForGrossRoC(portfolio, model.Value, TargetRoC, repCcy, hz.Value, LGD, xvaLgd, PartyRiskWeight, CVACapitalWeight, disc.Value, ContainerStores.CurrencyProvider);
            });
        }

        [ExcelFunction(Description = "Computes Basel II CVA risk weighted assets from an EPE profile", Category = CategoryNames.Capital, Name = CategoryNames.Capital + "_" + nameof(ComputeCvaRwaBaselII_IMM), IsThreadSafe = true)]
        public static object ComputeCvaRwaBaselII_IMM(
            [ExcelArgument(Description = "Origin date")] DateTime OriginDate,
            [ExcelArgument(Description = "EPE profile, cube or array")] object EPEProfile,
            [ExcelArgument(Description = "Loss-given-default, e.g. 0.4")] double LGD,
            [ExcelArgument(Description = "Probability-of-default, e.g. 0.02")] double PD)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                if (EPEProfile is object[,] arr)
                {
                    if (arr.GetLength(1) != 2)
                        throw new Exception("Expected Nx2 array for EPE");

                    var epeDates = new DateTime[arr.GetLength(0)];
                    var epeValues = new double[arr.GetLength(0)];
                    for (var i = 0; i < arr.GetLength(0); i++)
                    {
                        epeDates[i] = (DateTime)arr[i, 0];
                        epeValues[i] = (double)arr[i, 1];
                    }
                    return XVACalculator.RWA_BaselII_CCR_IMM(OriginDate, epeDates, epeValues, PD, LGD);
                }
                else if (EPEProfile is string epeCubeName)
                {
                    var cube = ContainerStores.GetObjectCache<ICube>().GetObjectOrThrow(epeCubeName, $"Cube {epeCubeName} not found");
                    return XVACalculator.RWA_BaselII_IMM(OriginDate, cube.Value, PD, LGD);
                }

                throw new Exception("EPE profile must be cube reference or Nx2 array");
            });
        }


        [ExcelFunction(Description = "Computes Basel III CVA capital from an EPE profile", Category = CategoryNames.Capital, Name = CategoryNames.Capital + "_" + nameof(ComputeCVACapitalBaselIII), IsThreadSafe = true)]
        public static object ComputeCVACapitalBaselIII(
           [ExcelArgument(Description = "Origin date")] DateTime OriginDate,
           [ExcelArgument(Description = "EPE profile, cube or array")] object EPEProfile,
           [ExcelArgument(Description = "Loss-given-default, e.g. 0.4")] double LGD,
           [ExcelArgument(Description = "Probability-of-default, e.g. 0.02")] double PD,
           [ExcelArgument(Description = "Counterparty weighting, e.g. 0.1")] double PartyWeight)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                if (EPEProfile is object[,] arr)
                {
                    if (arr.GetLength(1) != 2)
                        throw new Exception("Expected Nx2 array for EPE");

                    var epeDates = new DateTime[arr.GetLength(0)];
                    var epeValues = new double[arr.GetLength(0)];
                    for (var i = 0; i < arr.GetLength(0); i++)
                    {
                        epeDates[i] = (DateTime)arr[i, 0];
                        epeValues[i] = (double)arr[i, 1];
                    }
                    return XVACalculator.RWA_BaselIII_CVA_IMM(OriginDate, epeDates, epeValues, PD, LGD, PartyWeight);
                }
                else if (EPEProfile is string epeCubeName)
                {
                    var cube = ContainerStores.GetObjectCache<ICube>().GetObjectOrThrow(epeCubeName, $"Cube {epeCubeName} not found");
                    return XVACalculator.RWA_BaselIII_IMM(OriginDate, cube.Value, PD, LGD, PartyWeight);
                }

                throw new Exception("EPE profile must be cube reference or Nx2 array");
            });
        }

        [ExcelFunction(Description = "Returns PV capital / BaselII / IMM given an EAD profile and credit info", Category = CategoryNames.Capital, Name = CategoryNames.Capital + "_" + nameof(PortfolioPvCcrCapital))]
        public static object PortfolioPvCcrCapital(
            [ExcelArgument(Description = "Portfolio")] string Portfolio,
            [ExcelArgument(Description = "Expected EAD cube name")] string EADCubeName,
            [ExcelArgument(Description = "Credit settings object name")] string CreditSettingsName,
            [ExcelArgument(Description = "Origin date")] DateTime OriginDate)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var eadCube = ContainerStores.GetObjectCache<ICube>()
                    .GetObjectOrThrow(EADCubeName, $"Could not find cube with name {EADCubeName}");
                var creditSettings = ContainerStores.GetObjectCache<CreditSettings>()
                    .GetObjectOrThrow(CreditSettingsName, $"Could not find credit settings with name {CreditSettingsName}");
                var portfolio = Instruments.InstrumentFunctions.GetPortfolioOrTradeFromCache(Portfolio);

                var result = CapitalCalculator.PVCapital_BII_IMM(OriginDate, eadCube.Value, creditSettings.Value.CreditCurve, creditSettings.Value.BaseDiscountCurve, creditSettings.Value.LGD, portfolio);
                return result;
            });
        }

        [ExcelFunction(Description = "Returns PV CCR capital / BaselII / SM given portfolio, model and credit info", Category = CategoryNames.Capital, Name = CategoryNames.Capital + "_" + nameof(PortfolioPvCcrCapital_B2_SM))]
        public static object PortfolioPvCcrCapital_B2_SM(
            [ExcelArgument(Description = "Hazzard curve")] string HazzardCurveName,
            [ExcelArgument(Description = "Discount curve")] string DiscountCurve,
            [ExcelArgument(Description = "Portfolio")] string Portfolio,
            [ExcelArgument(Description = "Asset-FX Model")] string Model,
            [ExcelArgument(Description = "Loss-given-default, e.g. 0.4")] double LGD,
            [ExcelArgument(Description = "Reporting currency")] string Currency,
            [ExcelArgument(Description = "Exposure dates")] double[] ExposureDates)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var hz = ContainerStores.GetObjectCache<HazzardCurve>().GetObjectOrThrow(HazzardCurveName, $"Hazzard curve {HazzardCurveName} not found");
                var disc = ContainerStores.GetObjectCache<IIrCurve>().GetObjectOrThrow(DiscountCurve, $"Discount curve {DiscountCurve} not found");
                var portfolio = Instruments.InstrumentFunctions.GetPortfolioOrTradeFromCache(Portfolio);
                var model = ContainerStores.GetObjectCache<IAssetFxModel>().GetObjectOrThrow(Model, $"Asset-FX model {Model} not found");
                var repCcy = ContainerStores.CurrencyProvider.GetCurrency(Currency);
                var expDates = ExposureDates.ToDateTimeArray();
                var models = new IAssetFxModel[expDates.Length];
                var m = model.Value.Clone();
                for (var i=0;i<models.Length;i++)
                {
                    m = m.RollModel(expDates[i], ContainerStores.CurrencyProvider);
                    models[i] = m;
                }
                var result = CapitalCalculator.PvCcrCapital_BII_SM(model.Value.BuildDate, expDates, models, portfolio, hz.Value, repCcy, disc.Value, LGD);
                return result;
            });
        }

        [ExcelFunction(Description = "Returns PV CVA capital / BaselII / SM given portfolio, model and credit info", Category = CategoryNames.Capital, Name = CategoryNames.Capital + "_" + nameof(PortfolioPvCvaCapital_B2_SM))]
        public static object PortfolioPvCvaCapital_B2_SM(
            [ExcelArgument(Description = "Discount curve")] string DiscountCurve,
            [ExcelArgument(Description = "Portfolio")] string Portfolio,
            [ExcelArgument(Description = "Asset-FX Model")] string Model,
            [ExcelArgument(Description = "Reporting currency")] string Currency,
            [ExcelArgument(Description = "Exposure dates")] double[] ExposureDates,
            [ExcelArgument(Description = "Party risk weight")] double CvaRiskWeight)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var disc = ContainerStores.GetObjectCache<IIrCurve>().GetObjectOrThrow(DiscountCurve, $"Discount curve {DiscountCurve} not found");
                var portfolio = Instruments.InstrumentFunctions.GetPortfolioOrTradeFromCache(Portfolio);
                var model = ContainerStores.GetObjectCache<IAssetFxModel>().GetObjectOrThrow(Model, $"Asset-FX model {Model} not found");
                var repCcy = ContainerStores.CurrencyProvider.GetCurrency(Currency);
                var expDates = ExposureDates.ToDateTimeArray();
                var models = new IAssetFxModel[expDates.Length];
                var m = model.Value.Clone();
                for (var i = 0; i < models.Length; i++)
                {
                    m = m.RollModel(expDates[i], ContainerStores.CurrencyProvider);
                    models[i] = m;
                }
                var result = CapitalCalculator.PvCvaCapital_BII_SM(model.Value.BuildDate, expDates, models, portfolio, repCcy, disc.Value, CvaRiskWeight);
                return result;
            });
        }

        [ExcelFunction(Description = "Returns EAD profile / BaselII / SM given portfolio, model and credit info", Category = CategoryNames.Capital, Name = CategoryNames.Capital + "_" + nameof(PortfolioExpectedEad_B2_SM))]
        public static object PortfolioExpectedEad_B2_SM(
            [ExcelArgument(Description = "Portfolio")] string Portfolio,
            [ExcelArgument(Description = "Asset-FX Model")] string Model,
            [ExcelArgument(Description = "Reporting currency")] string Currency,
            [ExcelArgument(Description = "Exposure dates")] double[] ExposureDates)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var portfolio = Instruments.InstrumentFunctions.GetPortfolioOrTradeFromCache(Portfolio);
                var model = ContainerStores.GetObjectCache<IAssetFxModel>().GetObjectOrThrow(Model, $"Asset-FX model {Model} not found");
                var repCcy = ContainerStores.CurrencyProvider.GetCurrency(Currency);
                var expDates = ExposureDates.ToDateTimeArray();
                var models = new IAssetFxModel[expDates.Length];
                var m = model.Value.Clone();
                for (var i = 0; i < models.Length; i++)
                {
                    m = m.RollModel(expDates[i], ContainerStores.CurrencyProvider);
                    models[i] = m;
                }
                var result = CapitalCalculator.EAD_BII_SM(model.Value.BuildDate, expDates, models, portfolio, repCcy);
                return result;
            });
        }
    }
}
