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
using Qwack.Transport.BasicTypes;
using Qwack.Core.Basic;
using Qwack.Dates;
using Qwack.Core.Models;
using Qwack.Models.Calibrators;
using Qwack.Core.Instruments.Funding;
using Qwack.Models;

namespace Qwack.Excel.Curves
{

    public class InflationCurveFunctions
    {
        private static readonly ILogger _logger = ContainerStores.GlobalContainer.GetService<ILoggerFactory>()?.CreateLogger<InflationCurveFunctions>();

        [ExcelFunction(Description = "Creates a CPI curve from CPI forecasts", Category = CategoryNames.Curves, Name = CategoryNames.Curves + "_" + nameof(CreateCPICurveFromForecasts), IsThreadSafe = false)]
        public static object CreateCPICurveFromForecasts(
             [ExcelArgument(Description = "Object name")] string ObjectName,
             [ExcelArgument(Description = "Curve name")] object CurveName,
             [ExcelArgument(Description = "Build date")] DateTime BuildDate,
             [ExcelArgument(Description = "Array of pillar dates")] double[] Pillars,
             [ExcelArgument(Description = "Array of CPI forecasts")] double[] CPIForecasts,
             [ExcelArgument(Description = "Type of interpolation")] object InterpolationType,
             [ExcelArgument(Description = "Inflation Index")] string InfIndex,
             [ExcelArgument(Description = "Collateral Spec - default LIBOR.3M")] object CollateralSpec)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var curveName = CurveName.OptionalExcel(ObjectName);
                var curveTypeStr = InterpolationType.OptionalExcel("Linear");
                var colSpecStr = CollateralSpec.OptionalExcel("LIBOR.3M");

                if (!Enum.TryParse(curveTypeStr, out Interpolator1DType iType))
                {
                    return $"Could not parse interpolator type - {curveTypeStr}";
                }

                if (!ContainerStores.GetObjectCache<InflationIndex>().TryGetObject(InfIndex, out var rIndex))
                {
                    _logger?.LogInformation("Rate index {index} not found in cache", InfIndex);
                    return $"Rate index {InfIndex} not found in cache";
                }

                var pDates = Pillars.ToDateTimeArray();
                var cObj = new CPICurve(BuildDate, pDates, CPIForecasts, rIndex.Value)
                {
                    Name = curveName,
                };

                var cache = ContainerStores.GetObjectCache<IIrCurve>();
                cache.PutObject(ObjectName, new SessionItem<IIrCurve> { Name = ObjectName, Value = cObj });
                return ObjectName + '¬' + cache.GetObject(ObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Gets a CPI forecast from a curve", Category = CategoryNames.Curves, Name = CategoryNames.Curves + "_" + nameof(GetCpiForecast), IsThreadSafe = false)]
        public static object GetCpiForecast(
        [ExcelArgument(Description = "Curve object name")] string ObjectName,
        [ExcelArgument(Description = "Fixing Date")] DateTime FixingDate,
        [ExcelArgument(Description = "Fixing lag in months")] int LagInMonths)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                if (ContainerStores.GetObjectCache<IIrCurve>().TryGetObject(ObjectName, out var curve))
                {
                    if (curve.Value is CPICurve cpi)
                    {
                        return cpi.GetForecast(FixingDate, LagInMonths);
                    }

                    return $"Curve {ObjectName} is not a CPI Curve";
                }

                return $"CPI curve {ObjectName} not found in cache";
            });
        }
    }
}
