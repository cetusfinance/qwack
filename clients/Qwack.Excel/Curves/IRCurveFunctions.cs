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

    }
}
