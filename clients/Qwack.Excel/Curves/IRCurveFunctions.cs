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
             [ExcelArgument(Description = "Build date")] DateTime BuildDate,
             [ExcelArgument(Description = "Array of pillar dates")] double[] Pillars,
             [ExcelArgument(Description = "Array of CC zero rates")] double[] ZeroRates,
             [ExcelArgument(Description = "Type of interpolation")] object InterpolationType)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var curveTypeStr = InterpolationType.OptionalExcel<string>("Linear");
                if (!Enum.TryParse(curveTypeStr, out Interpolator1DType iType))
                {
                    return $"Could not parse price curve type - {curveTypeStr}";
                }

                var pDates = Pillars.ToDateTimeArray();
                var cObj = new IrCurve(pDates, ZeroRates, BuildDate, ObjectName, iType);
                var cache = ContainerStores.GetObjectCache<ICurve>();
                cache.PutObject(ObjectName, new SessionItem<ICurve> { Name = ObjectName, Value = cObj });
                return ObjectName + '¬' + cache.GetObject(ObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Creates a discount curve for fitting via a solver", Category = CategoryNames.Curves, Name = CategoryNames.Curves + "_" + nameof(CreateShellCurveForSolving))]
        public static object CreateShellCurveForSolving(
            [ExcelArgument(Description = "Object name")] string ObjectName,
            [ExcelArgument(Description = "Build date")] DateTime BuildDate,
            [ExcelArgument(Description = "Array of pillar dates")] double[] Pillars,
            [ExcelArgument(Description = "Solve Stage")] int SolveStage,
            [ExcelArgument(Description = "Type of interpolation")] object InterpolationType)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var curveTypeStr = InterpolationType.OptionalExcel<string>("Linear");
                if (!Enum.TryParse(curveTypeStr, out Interpolator1DType iType))
                {
                    return $"Could not parse price curve type - {curveTypeStr}";
                }

                var pDates = Pillars.ToDateTimeArray();
                var zeroRates = Pillars.Select(x => 0.01).ToArray();
                var cObj = new IrCurve(pDates, zeroRates, BuildDate, ObjectName, iType);
                cObj.SolveStage = SolveStage;
                var cache = ContainerStores.GetObjectCache<ICurve>();
                cache.PutObject(ObjectName, new SessionItem<ICurve> { Name = ObjectName, Value = cObj });
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
                if (ContainerStores.GetObjectCache<ICurve>().TryGetObject(ObjectName, out var curve))
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

                if (ContainerStores.GetObjectCache<ICurve>().TryGetObject(ObjectName, out var curve))
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
            [ExcelArgument(Description = "Fx matrix object")] object FxMatrix)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var ficCache = ContainerStores.GetObjectCache<FundingInstrumentCollection>();
                var fic = ficCache.GetObject(FundingInstrumentCollection).Value;

                var emptyCurves = fic.ImplyContainedCurves(BuildDate, Math.Interpolation.Interpolator1DType.Linear);

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
           [ExcelArgument(Description = "Curves")] object[] Curves)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var curveCache = ContainerStores.GetObjectCache<ICurve>();
                var curves = Curves
                    .Where(s => curveCache.Exists(s as string))
                    .Select(s => curveCache.GetObject(s as string).Value as IrCurve)
                    .ToArray();

                var fModel = new FundingModel(BuildDate, curves);

                var cache = ContainerStores.GetObjectCache<IFundingModel>();
                cache.PutObject(ObjectName, new SessionItem<IFundingModel> { Name = ObjectName, Value = fModel });
                return ObjectName + '¬' + cache.GetObject(ObjectName).Version;
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

                var curveCache = ContainerStores.GetObjectCache<ICurve>();
                curveCache.PutObject(OutputName, new SessionItem<ICurve> { Name = OutputName, Value = curve });
                return OutputName + '¬' + curveCache.GetObject(OutputName).Version;
            });
        }
    }
}
