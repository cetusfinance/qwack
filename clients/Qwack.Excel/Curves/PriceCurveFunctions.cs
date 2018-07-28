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

namespace Qwack.Excel.Curves
{
    public class PriceCurveFunctions
    {
        private static readonly ILogger _logger = ContainerStores.GlobalContainer.GetService<ILoggerFactory>()?.CreateLogger<PriceCurveFunctions>();

        [ExcelFunction(Description = "Creates a price curve", Category = CategoryNames.Curves, Name = CategoryNames.Curves + "_" + nameof(CreatePriceCurve))]
        public static object CreatePriceCurve(
             [ExcelArgument(Description = "Object name")] string ObjectName,
             [ExcelArgument(Description = "Build date")] DateTime BuildDate,
             [ExcelArgument(Description = "Array of pillar dates")] double[] Pillars,
             [ExcelArgument(Description = "Array of prices values")] double[] Prices,
             [ExcelArgument(Description = "Type of curve, e.g. LME, ICE, NYMEX etc")] object CurveType)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var curveTypeStr = CurveType.OptionalExcel<string>("Linear");
                if (!Enum.TryParse(curveTypeStr, out PriceCurveType cType))
                {
                    return $"Could not parse price curve type - {curveTypeStr}";
                }

                var pDates = Pillars.ToDateTimeArray();
                var cObj = new PriceCurve(BuildDate, pDates, Prices, cType);
                var cache = ContainerStores.GetObjectCache<IPriceCurve>();
                cache.PutObject(ObjectName, new SessionItem<IPriceCurve> { Name = ObjectName, Value = cObj });
                return ObjectName + '¬' + cache.GetObject(ObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Creates a sparse price curve", Category = CategoryNames.Curves, Name = CategoryNames.Curves + "_" + nameof(CreateSparsePriceCurve))]
        public static object CreateSparsePriceCurve(
            [ExcelArgument(Description = "Object name")] string ObjectName,
            [ExcelArgument(Description = "Build date")] DateTime BuildDate,
            [ExcelArgument(Description = "Array of pillar dates")] double[] Pillars,
            [ExcelArgument(Description = "Array of prices values")] double[] Prices,
            [ExcelArgument(Description = "Type of curve, e.g. Coal etc")] object CurveType)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var curveTypeStr = CurveType.OptionalExcel<string>("Coal");
                if (!Enum.TryParse(curveTypeStr, out SparsePriceCurveType cType))
                {
                    return $"Could not parse price curve type - {curveTypeStr}";
                }

                var pDates = Pillars.ToDateTimeArray();
                var cObj = new SparsePriceCurve(BuildDate, pDates, Prices, cType);
                var cache = ContainerStores.GetObjectCache<IPriceCurve>();
                cache.PutObject(ObjectName, new SessionItem<IPriceCurve> { Name = ObjectName, Value = cObj });
                return ObjectName + '¬' + cache.GetObject(ObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Queries a price curve for a price for a give date", Category = CategoryNames.Curves, Name = CategoryNames.Curves + "_" + nameof(GetPrice))]
        public static object GetPrice(
            [ExcelArgument(Description = "Object name")] string ObjectName,
            [ExcelArgument(Description = "Price date")] DateTime Date)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                if (ContainerStores.GetObjectCache<IPriceCurve>().TryGetObject(ObjectName, out var curve))
                {
                    return curve.Value.GetPriceForDate(Date);
                }

                return $"Price curve {ObjectName} not found in cache";
            });
        }

        [ExcelFunction(Description = "Queries a price curve for an average price for give dates", Category = CategoryNames.Curves, Name = CategoryNames.Curves + "_" + nameof(GetAveragePrice))]
        public static object GetAveragePrice(
            [ExcelArgument(Description = "Object name")] string ObjectName,
            [ExcelArgument(Description = "Price dates")] double[] Dates)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                if (ContainerStores.GetObjectCache<IPriceCurve>().TryGetObject(ObjectName, out var curve))
                {
                    return curve.Value.GetAveragePriceForDates(Dates.ToDateTimeArray());
                }

                return $"Price curve {ObjectName} not found in cache";
            });
        }
    }
}
