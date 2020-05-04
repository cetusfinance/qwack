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
using Qwack.Models.Calibrators;
using Qwack.Core.Instruments.Funding;
using Qwack.Models;

namespace Qwack.Excel.Curves
{
    public class IRCurveFunctions
    {
        private static readonly ILogger _logger = ContainerStores.GlobalContainer.GetService<ILoggerFactory>()?.CreateLogger<IRCurveFunctions>();

        [ExcelFunction(Description = "Creates a discount curve", Category = CategoryNames.Curves, Name = CategoryNames.Curves + "_" + nameof(CreateDiscountCurveFromCCRates), IsThreadSafe = false)]
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

        [ExcelFunction(Description = "Creates a discount curve", Category = CategoryNames.Curves, Name = CategoryNames.Curves + "_" + nameof(CreateDiscountCurveFromRates), IsThreadSafe = false)]
        public static object CreateDiscountCurveFromRates(
             [ExcelArgument(Description = "Object name")] string ObjectName,
             [ExcelArgument(Description = "Curve name")] object CurveName,
             [ExcelArgument(Description = "Build date")] DateTime BuildDate,
             [ExcelArgument(Description = "Array of pillar dates")] double[] Pillars,
             [ExcelArgument(Description = "Array of rates")] double[] ZeroRates,
             [ExcelArgument(Description = "Rate type, e.g. CC or Linear")] object RateType,
             [ExcelArgument(Description = "Type of interpolation, e.g. CubicSpline or Linear")] object InterpolationType,
             [ExcelArgument(Description = "Currency - default USD")] object Currency,
             [ExcelArgument(Description = "Collateral Spec - default LIBOR.3M")] object CollateralSpec)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var curveName = CurveName.OptionalExcel(ObjectName);
                var ccyStr = Currency.OptionalExcel("USD");
                var colSpecStr = CollateralSpec.OptionalExcel("LIBOR.3M");

                if (!Enum.TryParse(InterpolationType.OptionalExcel("Linear"), out Interpolator1DType iType))
                {
                    return $"Could not parse interpolator type - {InterpolationType}";
                }

                if (!Enum.TryParse(RateType.OptionalExcel("CC"), out RateType rType))
                {
                    return $"Could not parse rate type - {RateType}";
                }

                var pDates = Pillars.ToDateTimeArray();
                ContainerStores.SessionContainer.GetService<ICalendarProvider>().Collection.TryGetCalendar(ccyStr, out var ccyCal);
                var ccy = ContainerStores.GlobalContainer.GetRequiredService<ICurrencyProvider>().GetCurrency(ccyStr);

                var cObj = new IrCurve(pDates, ZeroRates, BuildDate, curveName, iType, ccy, colSpecStr, rType);
                return ExcelHelper.PushToCache<IIrCurve>(cObj, ObjectName);
            });
        }

        [ExcelFunction(Description = "Creates a discount curve", Category = CategoryNames.Curves, Name = CategoryNames.Curves + "_" + nameof(CreateDiscountCurveFromDFs), IsThreadSafe = false)]
        public static object CreateDiscountCurveFromDFs(
             [ExcelArgument(Description = "Object name")] string ObjectName,
             [ExcelArgument(Description = "Curve name")] object CurveName,
             [ExcelArgument(Description = "Build date")] DateTime BuildDate,
             [ExcelArgument(Description = "Array of pillar dates")] double[] Pillars,
             [ExcelArgument(Description = "Array of discount factors")] double[] DiscountFactors,
             [ExcelArgument(Description = "Type of interpolation")] object InterpolationType,
             [ExcelArgument(Description = "Currency - default USD")] object Currency,
             [ExcelArgument(Description = "Collateral Spec - default LIBOR.3M")] object CollateralSpec,
             [ExcelArgument(Description = "Rate storage format - default Exponential")] object RateStorageType)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var curveName = CurveName.OptionalExcel(ObjectName);
                var curveTypeStr = InterpolationType.OptionalExcel("Linear");
                var ccyStr = Currency.OptionalExcel("USD");
                var colSpecStr = CollateralSpec.OptionalExcel("LIBOR.3M");
                var rateTypeStr = RateStorageType.OptionalExcel("CC");

                if (!Enum.TryParse(curveTypeStr, out Interpolator1DType iType))
                {
                    return $"Could not parse interpolator type - {curveTypeStr}";
                }

                if (!Enum.TryParse(rateTypeStr, out RateType rType))
                {
                    return $"Could not parse rate type - {rateTypeStr}";
                }

                var pDates = Pillars.ToDateTimeArray();
                ContainerStores.SessionContainer.GetService<ICalendarProvider>().Collection.TryGetCalendar(ccyStr, out var ccyCal);
                var ccy = ContainerStores.GlobalContainer.GetRequiredService<ICurrencyProvider>()[ccyStr];

                var zeroRates = DiscountFactors
                .Select((df, ix) => 
                    DateTime.FromOADate(Pillars[ix])==BuildDate ? 0.0 
                    : IrCurve.RateFromDF(BuildDate.CalculateYearFraction(DateTime.FromOADate(Pillars[ix]), DayCountBasis.ACT365F),df, rType))
                .ToArray();

                if (DateTime.FromOADate(Pillars[0]) == BuildDate && zeroRates.Length > 1)
                    zeroRates[0] = zeroRates[1];

                var cObj = new IrCurve(pDates, zeroRates, BuildDate, curveName, iType, ccy, colSpecStr, rType);
                return ExcelHelper.PushToCache<IIrCurve>(cObj, ObjectName);
            });
        }

        [ExcelFunction(Description = "Gets a discount factor from a curve", Category = CategoryNames.Curves, Name = CategoryNames.Curves + "_" + nameof(GetDF), IsThreadSafe = false)]
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

        [ExcelFunction(Description = "Gets a forward rate from a curve", Category = CategoryNames.Curves, Name = CategoryNames.Curves + "_" + nameof(GetForwardRate), IsThreadSafe = false)]
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

        [ExcelFunction(Description = "Gets a forward fx rate from a funding model", Category = CategoryNames.Curves, Name = CategoryNames.Curves + "_" + nameof(GetForwardFxRate), IsThreadSafe = false)]
        public static object GetForwardFxRate(
            [ExcelArgument(Description = "Funding model object name")] string ObjectName,
            [ExcelArgument(Description = "Settlement date")] DateTime SettleDate,
            [ExcelArgument(Description = "Currency pair")] string CcyPair)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                if (!ContainerStores.GetObjectCache<IFundingModel>().TryGetObject(ObjectName, out var model))
                {
                    return $"Funding model with name {ObjectName} not found";
                }

                var fwd = model.Value.GetFxRate(SettleDate, CcyPair);
                return fwd;
            });
        }

        [ExcelFunction(Description = "Creates and calibrates a funding model to a funding instrument collection", Category = CategoryNames.Curves, Name = CategoryNames.Curves + "_" + nameof(CreateFundingModel), IsThreadSafe = false)]
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
                ContainerStores.GetObjectCache<FundingInstrumentCollection>().TryGetObject(FundingInstrumentCollection, out var fic);

                IFxMatrix fxMatrix = null;
                if (!(FxMatrix is ExcelMissing))
                {
                    var fxMatrixCache = ContainerStores.GetObjectCache<FxMatrix>();
                    fxMatrix = fxMatrixCache.GetObject((string)FxMatrix).Value;
                }

                var emptyCurves = new Dictionary<string, IrCurve>();
                if (fic != null)
                {
                    emptyCurves = fic.Value.ImplyContainedCurves(BuildDate, Interpolator1DType.LinearFlatExtrap);

                    var stageDict = !(SolveStages is ExcelMissing)
                        ? ((object[,])SolveStages).RangeToDictionary<string, int>()
                        : fic.Value.ImplySolveStages(fxMatrix);

                    foreach (var kv in stageDict)
                    {
                        if (emptyCurves.TryGetValue(kv.Key, out var curve))
                        {
                            curve.SolveStage = kv.Value;
                        }
                        else
                        {
                            throw new Exception($"Solve stage specified for curve {kv.Key} but curve not present");
                        }
                    }
                }

                var model = new FundingModel(BuildDate, emptyCurves.Values.ToArray(), ContainerStores.CurrencyProvider, ContainerStores.CalendarProvider);
                
                if (!(FxMatrix is ExcelMissing))
                    model.SetupFx(fxMatrix);

                if (!(FxVolSurfaces is ExcelMissing))
                {
                    IEnumerable<IVolSurface> surfaces = null;
                    if (FxVolSurfaces is string vsStr)
                        surfaces = (new object[] { vsStr }).GetAnyFromCache<IVolSurface>();
                    else
                        surfaces = ((object[,])FxVolSurfaces).GetAnyFromCache<IVolSurface>();
                    if (surfaces.Any())
                        model.VolSurfaces = surfaces.ToDictionary(k => k.Name, v => v);
                }

                if (fic != null)
                {
                    var calibrator = new NewtonRaphsonMultiCurveSolverStaged()
                    {
                        InLineCurveGuessing = true
                    };
                    calibrator.Solve(model, fic.Value);
                }

                return ExcelHelper.PushToCache<IFundingModel>(model, ObjectName);
            });
        }

        [ExcelFunction(Description = "Creates and calibrates a funding model to a funding instrument collection", Category = CategoryNames.Curves, Name = CategoryNames.Curves + "_" + nameof(CreateFundingModelParallel), IsThreadSafe = false)]
        public static object CreateFundingModelParallel(
           [ExcelArgument(Description = "Funding model name")] string ObjectName,
           [ExcelArgument(Description = "Build date")] DateTime BuildDate,
           [ExcelArgument(Description = "Funding instrument collection")] string FundingInstrumentCollection,
           [ExcelArgument(Description = "Curve to solve stage mappings")] object SolveStages,
           [ExcelArgument(Description = "Fx matrix object")] object FxMatrix,
           [ExcelArgument(Description = "Fx vol surfaces")] object FxVolSurfaces)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                ContainerStores.GetObjectCache<FundingInstrumentCollection>().TryGetObject(FundingInstrumentCollection, out var fic);

                IFxMatrix fxMatrix = null;
                if (!(FxMatrix is ExcelMissing))
                {
                    var fxMatrixCache = ContainerStores.GetObjectCache<FxMatrix>();
                    fxMatrix = fxMatrixCache.GetObject((string)FxMatrix).Value;
                }

                var stageDict = fic.Value.ImplySolveStages2(fxMatrix);
                var emptyCurves = fic.Value.ImplyContainedCurves(BuildDate, Interpolator1DType.LinearFlatExtrap);

                var model = new FundingModel(BuildDate, emptyCurves.Values.ToArray(), ContainerStores.CurrencyProvider, ContainerStores.CalendarProvider);

                if (!(FxMatrix is ExcelMissing))
                    model.SetupFx(fxMatrix);

                if (!(FxVolSurfaces is ExcelMissing))
                {
                    IEnumerable<IVolSurface> surfaces = null;
                    if (FxVolSurfaces is string vsStr)
                        surfaces = (new object[] { vsStr }).GetAnyFromCache<IVolSurface>();
                    else
                        surfaces = ((object[,])FxVolSurfaces).GetAnyFromCache<IVolSurface>();
                    if (surfaces.Any())
                        model.VolSurfaces = surfaces.ToDictionary(k => k.Name, v => v);
                }

                if (fic != null)
                {
                    var calibrator = new NewtonRaphsonMultiCurveSolverStaged()
                    {
                        InLineCurveGuessing = true
                    };
                    calibrator.Solve(model, fic.Value, stageDict);
                }

                return ExcelHelper.PushToCache<IFundingModel>(model, ObjectName);
            });
        }


        [ExcelFunction(Description = "Creates a funding model from one or more curves", Category = CategoryNames.Curves, Name = CategoryNames.Curves + "_" + nameof(CreateFundingModelFromCurves), IsThreadSafe = false)]
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

                var fModel = new FundingModel(BuildDate, curves, ContainerStores.CurrencyProvider, ContainerStores.CalendarProvider);

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

                return ExcelHelper.PushToCache<IFundingModel>(fModel, ObjectName);
            });
        }

        [ExcelFunction(Description = "Creates a new funding model by combining two others", Category = CategoryNames.Curves, Name = CategoryNames.Curves + "_" + nameof(MergeFundingModels), IsThreadSafe = false)]
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

                var outModel = new FundingModel(modelA.Value.BuildDate, combinedCurves, ContainerStores.CurrencyProvider, ContainerStores.CalendarProvider);

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

                var fxMatrix = new FxMatrix(ContainerStores.CurrencyProvider);
                fxMatrix.Init(fxA.BaseCurrency, modelA.Value.BuildDate, spotRates, pairs, discoMap);

                outModel.SetupFx(fxMatrix);

                return ExcelHelper.PushToCache<IFundingModel>(outModel, ObjectName);
            });
        }

        [ExcelFunction(Description = "Lists curves in a funding model", Category = CategoryNames.Curves, Name = CategoryNames.Curves + "_" + nameof(ListCurvesInModel), IsThreadSafe = false)]
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

        [ExcelFunction(Description = "Extracts a curve from a funding model", Category = CategoryNames.Curves, Name = CategoryNames.Curves + "_" + nameof(ExtractCurveFromModel), IsThreadSafe = false)]
        public static object ExtractCurveFromModel(
           [ExcelArgument(Description = "Funding model name")] string FundingModelName,
           [ExcelArgument(Description = "Curve name")]  string CurveName,
           [ExcelArgument(Description = "Output curve object name")] string OutputName)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var model = ContainerStores.GetObjectCache<IFundingModel>().GetObject(FundingModelName).Value;

                return model.Curves.TryGetValue(CurveName, out var curve) ?
                    ExcelHelper.PushToCache<IIrCurve>(curve, OutputName) :
                    $"Curve {CurveName} not found in model";
            });
        }

        [ExcelFunction(Description = "Extracts a calibration info from a funding model", Category = CategoryNames.Curves, Name = CategoryNames.Curves + "_" + nameof(ExtractCalibrationInfoFromModel), IsThreadSafe = false)]
        public static object ExtractCalibrationInfoFromModel(
           [ExcelArgument(Description = "Funding model name")] string FundingModelName)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var model = ContainerStores.GetObjectCache<IFundingModel>().GetObject(FundingModelName).Value;

                if (model.CalibrationItterations != null)
                {
                    var o = new object[model.CalibrationItterations.Count() + 1, 3];

                    o[0, 0] = "Time (ms)";
                    o[0, 1] = model.CalibrationTimeMs;
                    o[0, 2] = "Curves in stage";

                    for (var i = 1; i <= model.CalibrationItterations.Count(); i++)
                    {
                        o[i, 0] = $"Passes Stage {i - 1}";
                        o[i, 1] = model.CalibrationItterations[i - 1];
                        o[i, 2] = model.CalibrationCurves[i - 1];
                    }

                    return o;
                }
                else
                {
                    var o = new object[1, 2];

                    o[0, 0] = "Time (ms)";
                    o[0, 1] = model.CalibrationTimeMs;

                    return o;
                }
            });
        }
    }
}
