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

     
    }
}
