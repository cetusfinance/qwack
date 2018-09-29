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
using Qwack.Core.Models;
using Qwack.Core.Calibrators;
using Qwack.Core.Instruments.Funding;
using Qwack.Models;

namespace Qwack.Excel.Curves
{
    public class IRCurveFunctions
    {
        private static readonly ILogger _logger = ContainerStores.GlobalContainer.GetService<ILoggerFactory>()?.CreateLogger<IRCurveFunctions>();

        [ExcelFunction(Description = "Creates a discount curve", Category = CategoryNames.Curves, Name = CategoryNames.Curves + "_" + nameof(CreateDiscountCurveFromCCRates))]
        public static object CreateDiscountCurveFromCCRates(
             [ExcelArgument(Description = "Object name")] string ObjectName,
             [ExcelArgument(Description = "Curve name")] object CurveName,
             [ExcelArgument(Description = "Build date")] DateTime BuildDate,
             [ExcelArgument(Description = "Array of pillar dates")] double[] Pillars,
             [ExcelArgument(Description = "Array of CC zero rates")] double[] ZeroRates,
             [ExcelArgument(Description = "Type of interpolation")] object InterpolationType,
             [ExcelArgument(Description = "Currency - default USD")] object Currency,
             [ExcelArgument(Description = "Collateral Spec - default LIBOR.3M")] object CollateralSpec)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var curveName = CurveName.OptionalExcel(ObjectName);
                var curveTypeStr = InterpolationType.OptionalExcel("Linear");
                var ccyStr = Currency.OptionalExcel("USD");
                var colSpecStr = CollateralSpec.OptionalExcel("LIBOR.3M");

                if (!Enum.TryParse(curveTypeStr, out Interpolator1DType iType))
                {
                    return $"Could not parse interpolator type - {curveTypeStr}";
                }

                var pDates = Pillars.ToDateTimeArray();
                ContainerStores.SessionContainer.GetService<ICalendarProvider>().Collection.TryGetCalendar(ccyStr, out var ccyCal);
                var ccy = ContainerStores.GlobalContainer.GetRequiredService<ICurrencyProvider>()[ccyStr];

                var cObj = new IrCurve(pDates, ZeroRates, BuildDate, curveName, iType, ccy, colSpecStr);
                var cache = ContainerStores.GetObjectCache<IIrCurve>();
                cache.PutObject(ObjectName, new SessionItem<IIrCurve> { Name = ObjectName, Value = cObj });
                return ObjectName + '¬' + cache.GetObject(ObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Creates a discount curve", Category = CategoryNames.Curves, Name = CategoryNames.Curves + "_" + nameof(CreateDiscountCurveFromDFs))]
        public static object CreateDiscountCurveFromDFs(
             [ExcelArgument(Description = "Object name")] string ObjectName,
             [ExcelArgument(Description = "Curve name")] object CurveName,
             [ExcelArgument(Description = "Build date")] DateTime BuildDate,
             [ExcelArgument(Description = "Array of pillar dates")] double[] Pillars,
             [ExcelArgument(Description = "Array of discount factors")] double[] DiscountFactors,
             [ExcelArgument(Description = "Type of interpolation")] object InterpolationType,
             [ExcelArgument(Description = "Currency - default USD")] object Currency,
             [ExcelArgument(Description = "Collateral Spec - default LIBOR.3M")] object CollateralSpec)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var curveName = CurveName.OptionalExcel(ObjectName);
                var curveTypeStr = InterpolationType.OptionalExcel("Linear");
                var ccyStr = Currency.OptionalExcel("USD");
                var colSpecStr = CollateralSpec.OptionalExcel("LIBOR.3M");

                if (!Enum.TryParse(curveTypeStr, out Interpolator1DType iType))
                {
                    return $"Could not parse interpolator type - {curveTypeStr}";
                }

                var pDates = Pillars.ToDateTimeArray();
                ContainerStores.SessionContainer.GetService<ICalendarProvider>().Collection.TryGetCalendar(ccyStr, out var ccyCal);
                var ccy = ContainerStores.GlobalContainer.GetRequiredService<ICurrencyProvider>()[ccyStr];

                var zeroRates = DiscountFactors
                .Select((df, ix) => DateTime.FromOADate(Pillars[ix])==BuildDate ? 0.0 : -System.Math.Log(df) / BuildDate.CalculateYearFraction(DateTime.FromOADate(Pillars[ix]), DayCountBasis.ACT365F))
                .ToArray();

                if (DateTime.FromOADate(Pillars[0]) == BuildDate && zeroRates.Length > 1)
                    zeroRates[0] = zeroRates[1];

                var cObj = new IrCurve(pDates, zeroRates, BuildDate, curveName, iType, ccy, colSpecStr);
                var cache = ContainerStores.GetObjectCache<IIrCurve>();
                cache.PutObject(ObjectName, new SessionItem<IIrCurve> { Name = ObjectName, Value = cObj });
                return ObjectName + '¬' + cache.GetObject(ObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Gets a discount factor from a curve", Category = CategoryNames.Curves, Name = CategoryNames.Curves + "_" + nameof(GetDF))]
        public static object GetDF(
          [ExcelArgument(Description = "Curve object name")] string ObjectName,
          [ExcelArgument(Description = "Discount factor start date")] DateTime StartDate,
          [ExcelArgument(Description = "Discount factor end date")] DateTime EndDate)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                if (ContainerStores.GetObjectCache<IIrCurve>().TryGetObject(ObjectName, out var curve))
                {
                    return curve.Value.GetDf(StartDate,EndDate);
                }

                return $"IR curve {ObjectName} not found in cache";
            });
        }

        [ExcelFunction(Description = "Gets a forward rate from a curve", Category = CategoryNames.Curves, Name = CategoryNames.Curves + "_" + nameof(GetForwardRate))]
        public static object GetForwardRate(
            [ExcelArgument(Description = "Curve object name")] string ObjectName,
            [ExcelArgument(Description = "Rate start date")] DateTime StartDate,
            [ExcelArgument(Description = "Rate end date")] DateTime EndDate,
            [ExcelArgument(Description = "Rate type, e.g. linear, CC")] object RateType,
            [ExcelArgument(Description = "Basis, e.g. Act365F")] object Basis)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                if (StartDate >= EndDate)
                    return "End date must be strictly greater that start date";

                var rateType = RateType.OptionalExcel<string>("Linear");
                var basis = Basis.OptionalExcel<string>("Act365F");

                if (!Enum.TryParse(rateType, out RateType rType))
                {
                    return $"Could not parse rate type - {rateType}";
                }
                if (!Enum.TryParse(basis, out DayCountBasis dType))
                {
                    return $"Could not daycount basis - {basis}";
                }

                if (ContainerStores.GetObjectCache<IIrCurve>().TryGetObject(ObjectName, out var curve))
                {
                    return curve.Value.GetForwardRate(StartDate, EndDate, rType, dType);
                }

                return $"IR curve {ObjectName} not found in cache";
            });
        }

        [ExcelFunction(Description = "Creates and calibrates a funding model to a funding instrument collection", Category = CategoryNames.Curves, Name = CategoryNames.Curves + "_" + nameof(CreateFundingModel))]
        public static object CreateFundingModel(
            [ExcelArgument(Description = "Funding model name")] string ObjectName,
            [ExcelArgument(Description = "Build date")] DateTime BuildDate,
            [ExcelArgument(Description = "Funding instrument collection")] string FundingInstrumentCollection,
            [ExcelArgument(Description = "Curve to solve stage mappings")] object SolveStages,
            [ExcelArgument(Description = "Fx matrix object")] object FxMatrix,
            [ExcelArgument(Description = "Fx vol surfaces")] object FxVolSurfaces)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var ficCache = ContainerStores.GetObjectCache<FundingInstrumentCollection>();
                var fic = ficCache.GetObject(FundingInstrumentCollection).Value;

                var emptyCurves = fic.ImplyContainedCurves(BuildDate, Interpolator1DType.Linear);

                if(!(SolveStages is ExcelMissing))
                {
                    var stageDict = ((object[,])SolveStages).RangeToDictionary<string,int>();
                    foreach(var kv in stageDict)
                    {
                        if(emptyCurves.TryGetValue(kv.Key, out var curve))
                        {
                            curve.SolveStage = kv.Value;
                        }
                        else
                        {
                            throw new Exception($"Solve stage specified for curve {kv.Key} but curve not present");
                        }
                    }
                }

                var model = new FundingModel(BuildDate, emptyCurves.Values.ToArray());

                if(!(FxMatrix is ExcelMissing))
                {
                    var fxMatrixCache = ContainerStores.GetObjectCache<FxMatrix>();
                    var fxMatrix = fxMatrixCache.GetObject((string)FxMatrix);
                    model.SetupFx(fxMatrix.Value);
                }

                if (!(FxVolSurfaces is ExcelMissing))
                {
                    var surfaces = (new object[] { FxVolSurfaces }).GetAnyFromCache<IVolSurface>();
                    if (surfaces.Any())
                        model.VolSurfaces = surfaces.ToDictionary(k => k.Name, v => v);
                }


                var calibrator = new NewtonRaphsonMultiCurveSolverStaged();
                calibrator.Solve(model, fic);

                var modelCache = ContainerStores.GetObjectCache<IFundingModel>();
                modelCache.PutObject(ObjectName, new SessionItem<IFundingModel> { Name = ObjectName, Value = model });
                return ObjectName + '¬' + modelCache.GetObject(ObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Creates a funding model from one or more curves", Category = CategoryNames.Curves, Name = CategoryNames.Curves + "_" + nameof(CreateFundingModelFromCurves))]
        public static object CreateFundingModelFromCurves(
           [ExcelArgument(Description = "Output funding model")] string ObjectName,
           [ExcelArgument(Description = "Build date")] DateTime BuildDate,
           [ExcelArgument(Description = "Curves")] object[] Curves,
           [ExcelArgument(Description = "Fx matrix object")] object FxMatrix,
           [ExcelArgument(Description = "Fx vol surfaces")] object FxVolSurfaces)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var curveCache = ContainerStores.GetObjectCache<IIrCurve>();
                var curves = Curves
                    .Where(s => curveCache.Exists(s as string))
                    .Select(s => curveCache.GetObject(s as string).Value as IrCurve)
                    .ToArray();

                var fModel = new FundingModel(BuildDate, curves, ContainerStores.CurrencyProvider);

                if (!(FxMatrix is ExcelMissing))
                {
                    var fxMatrixCache = ContainerStores.GetObjectCache<FxMatrix>();
                    var fxMatrix = fxMatrixCache.GetObject((string)FxMatrix);
                    fModel.SetupFx(fxMatrix.Value);
                }

                if (!(FxVolSurfaces is ExcelMissing))
                {
                    var surfaces = (new object[] { FxVolSurfaces }).GetAnyFromCache<IVolSurface>();
                    if (surfaces.Any())
                        fModel.VolSurfaces = surfaces.ToDictionary(k => k.Name, v => v);
                }

                var cache = ContainerStores.GetObjectCache<IFundingModel>();
                cache.PutObject(ObjectName, new SessionItem<IFundingModel> { Name = ObjectName, Value = fModel });
                return ObjectName + '¬' + cache.GetObject(ObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Creates a new funding model by combining two others", Category = CategoryNames.Curves, Name = CategoryNames.Curves + "_" + nameof(MergeFundingModels))]
        public static object MergeFundingModels(
           [ExcelArgument(Description = "Output funding model")] string ObjectName,
           [ExcelArgument(Description = "Funding model A")] string FundingModelA,
           [ExcelArgument(Description = "Funding model B")] string FundingModelB)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var modelCache = ContainerStores.GetObjectCache<IFundingModel>();
                if(!modelCache.TryGetObject(FundingModelA,out var modelA))
                {
                    return $"Could not find funding model {FundingModelA}";
                }
                if (!modelCache.TryGetObject(FundingModelB, out var modelB))
                {
                    return $"Could not find funding model {FundingModelB}";
                }

                var combinedCurves = modelA.Value.Curves.Values.Concat(modelB.Value.Curves.Values).ToArray();

                if(combinedCurves.Length != combinedCurves.Select(x=>x.Name).Distinct().Count())
                {
                    return $"Not all curves have unique names";
                }

                var currencies = ContainerStores.GlobalContainer.GetRequiredService<ICurrencyProvider>();
                var outModel = new FundingModel(modelA.Value.BuildDate, combinedCurves, currencies);

                foreach (var vs in modelA.Value.VolSurfaces)
                    outModel.VolSurfaces.Add(vs.Key, vs.Value);
                foreach (var vs in modelB.Value.VolSurfaces)
                    outModel.VolSurfaces.Add(vs.Key, vs.Value);

                var fxA = modelA.Value.FxMatrix;
                var fxB = modelB.Value.FxMatrix;

                var spotRates = new Dictionary<Currency, double>();
                foreach (var s in fxA.SpotRates)
                    spotRates.Add(s.Key, s.Value);
                foreach (var s in fxB.SpotRates)
                    spotRates.Add(s.Key, s.Value);

                var discoMap = new Dictionary<Currency, string>();
                foreach (var s in fxA.DiscountCurveMap)
                    discoMap.Add(s.Key, s.Value);
                foreach (var s in fxB.DiscountCurveMap)
                    discoMap.Add(s.Key, s.Value);

                var pairs = fxA.FxPairDefinitions.Concat(fxB.FxPairDefinitions).Distinct().ToList();

                var fxMatrix = new FxMatrix(currencies);
                fxMatrix.Init(fxA.BaseCurrency, modelA.Value.BuildDate, spotRates, pairs, discoMap);

                outModel.SetupFx(fxMatrix);

                modelCache.PutObject(ObjectName, new SessionItem<IFundingModel> { Name = ObjectName, Value = outModel });
                return ObjectName + '¬' + modelCache.GetObject(ObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Lists curves in a funding model", Category = CategoryNames.Curves, Name = CategoryNames.Curves + "_" + nameof(ListCurvesInModel))]
        public static object ListCurvesInModel(
          [ExcelArgument(Description = "Funding model name")] string FundingModelName)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var modelCache = ContainerStores.GetObjectCache<IFundingModel>();
                var model = modelCache.GetObject(FundingModelName).Value;

                return model.Curves.Keys.Select(x=>x as string).ToArray().ReturnExcelRangeVector();
            });
        }

        [ExcelFunction(Description = "Extracts a curve from a funding model", Category = CategoryNames.Curves, Name = CategoryNames.Curves + "_" + nameof(ExtractCurveFromModel))]
        public static object ExtractCurveFromModel(
           [ExcelArgument(Description = "Funding model name")] string FundingModelName,
           [ExcelArgument(Description = "Curve name")]  string CurveName,
           [ExcelArgument(Description = "Output curve object name")] string OutputName)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var modelCache = ContainerStores.GetObjectCache<IFundingModel>();
                var model = modelCache.GetObject(FundingModelName).Value;

                if (!model.Curves.TryGetValue(CurveName, out var curve))
                {
                    return $"Curve {CurveName} not found in model";
                }

                var curveCache = ContainerStores.GetObjectCache<IIrCurve>();
                curveCache.PutObject(OutputName, new SessionItem<IIrCurve> { Name = OutputName, Value = curve });
                return OutputName + '¬' + curveCache.GetObject(OutputName).Version;
            });
        }
    }
}
