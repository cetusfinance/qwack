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
using Qwack.Core.Instruments.Asset;
using Qwack.Core.Models;
using Qwack.Models;
using Qwack.Core.Basic;
using Qwack.Models.Models;

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
             [ExcelArgument(Description = "Type of curve, e.g. LME, ICE, NYMEX etc")] object CurveType,
             [ExcelArgument(Description = "Array of pillar labels (optional)")] object PillarLabels)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var curveTypeStr = CurveType.OptionalExcel<string>("Linear");
                if (!Enum.TryParse(curveTypeStr, out PriceCurveType cType))
                {
                    return $"Could not parse price curve type - {curveTypeStr}";
                }

                var labels = (PillarLabels is ExcelMissing) ? null : ((object[,])PillarLabels).ObjectRangeToVector<string>();

                var pDates = Pillars.ToDateTimeArray();
                var cObj = new PriceCurve(BuildDate, pDates, Prices, cType, labels)
                {
                    Name = ObjectName
                };
                var cache = ContainerStores.GetObjectCache<IPriceCurve>();
                cache.PutObject(ObjectName, new SessionItem<IPriceCurve> { Name = ObjectName, Value = cObj });
                return ObjectName + '¬' + cache.GetObject(ObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Creates a contango price curve for precious metals", Category = CategoryNames.Curves, Name = CategoryNames.Curves + "_" + nameof(CreateContangoPriceCurve))]
        public static object CreateContangoPriceCurve(
            [ExcelArgument(Description = "Object name")] string ObjectName,
            [ExcelArgument(Description = "Build date")] DateTime BuildDate,
            [ExcelArgument(Description = "Spot date")] DateTime SpotDate,
            [ExcelArgument(Description = "Spot price")] double SpotPrice,
            [ExcelArgument(Description = "Array of pillar dates")] double[] Pillars,
            [ExcelArgument(Description = "Array of contango values")] double[] ContangoRates,
            [ExcelArgument(Description = "Array of pillar labels (optional)")] object PillarLabels)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var labels = (PillarLabels is ExcelMissing) ? null : ((object[,])PillarLabels).ObjectRangeToVector<string>();
                var pDates = Pillars.ToDateTimeArray();
                var cObj = new ContangoPriceCurve(BuildDate, SpotPrice, SpotDate, pDates, ContangoRates, Qwack.Dates.DayCountBasis.Act360, labels)
                {
                    Name = ObjectName
                };
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
                var cObj = new SparsePriceCurve(BuildDate, pDates, Prices, cType)
                {
                    Name = ObjectName
                };
                var cache = ContainerStores.GetObjectCache<IPriceCurve>();
                cache.PutObject(ObjectName, new SessionItem<IPriceCurve> { Name = ObjectName, Value = cObj });
                return ObjectName + '¬' + cache.GetObject(ObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Creates a sparse price curve from swap objects", Category = CategoryNames.Curves, Name = CategoryNames.Curves + "_" + nameof(CreateSparsePriceCurveFromSwaps))]
        public static object CreateSparsePriceCurveFromSwaps(
            [ExcelArgument(Description = "Object name")] string ObjectName,
            [ExcelArgument(Description = "Build date")] DateTime BuildDate,
            [ExcelArgument(Description = "Array of pillar dates")] double[] Pillars,
            [ExcelArgument(Description = "Array of swap objects")] object[] Swaps,
            [ExcelArgument(Description = "Discount curve name")] string DiscountCurveName,
            [ExcelArgument(Description = "Type of curve, e.g. Coal etc")] object CurveType)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var curveTypeStr = CurveType.OptionalExcel<string>("Coal");
                if (!Enum.TryParse(curveTypeStr, out SparsePriceCurveType cType))
                {
                    return $"Could not parse price curve type - {curveTypeStr}";
                }

                var irCache = ContainerStores.GetObjectCache<ICurve>();
                var irCurve = irCache.GetObject(DiscountCurveName).Value;

                var swapCache = ContainerStores.GetObjectCache<AsianSwapStrip>();
                var swaps = Swaps.Select(s => swapCache.GetObject(s as string)).Select(x => x.Value);

                var pDates = Pillars.ToDateTimeArray();
                var fitter = new Qwack.Core.Calibrators.NewtonRaphsonAssetCurveSolver();
                var cObj = fitter.Solve(swaps.ToList(), pDates.ToList(), irCurve, BuildDate);
                cObj.Name = ObjectName;
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

        [ExcelFunction(Description = "Creates a new Asset-FX model", Category = CategoryNames.Curves, Name = CategoryNames.Curves + "_" + nameof(CreateAssetFxModel))]
        public static object CreateAssetFxModel(
            [ExcelArgument(Description = "Object name")] string ObjectName,
            [ExcelArgument(Description = "Build date")] DateTime BuildDate,
            [ExcelArgument(Description = "Price curves")] object[] PriceCurves,
            [ExcelArgument(Description = "Vol surfaces")] object[] VolSurfaces,
            [ExcelArgument(Description = "Funding model")] object[] FundingModel,
            [ExcelArgument(Description = "Fixing dictionaries")] object[] Fixings,
            [ExcelArgument(Description = "Correlation matrix")] object[] CorrelationMatrix)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var curves = PriceCurves.GetAnyFromCache<IPriceCurve>();
                var fundingModel = FundingModel.GetAnyFromCache<IFundingModel>().First();
                var fixings = Fixings.GetAnyFromCache<IFixingDictionary>();
                var volSurfaces = VolSurfaces.GetAnyFromCache<IVolSurface>();
                var correlatinMatrix = CorrelationMatrix.GetAnyFromCache<ICorrelationMatrix>();

                var model = new AssetFxModel(BuildDate, fundingModel);

                foreach (var curve in curves)
                    model.AddPriceCurve(curve.Name, curve);
                foreach (var vs in volSurfaces)
                    model.AddVolSurface(vs.Name, vs);
                foreach (var f in fixings)
                    model.AddFixingDictionary(f.Name, f);

                if(correlatinMatrix.Any())
                {
                    model.CorrelationMatrix = correlatinMatrix.First();
                }

                var cache = ContainerStores.GetObjectCache<IAssetFxModel>();
                cache.PutObject(ObjectName, new SessionItem<IAssetFxModel> { Name = ObjectName, Value = model });
                return ObjectName + '¬' + cache.GetObject(ObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Creates a fixing dictionary", Category = CategoryNames.Curves, Name = CategoryNames.Curves + "_" + nameof(CreateFixingDictionary))]
        public static object CreateFixingDictionary(
            [ExcelArgument(Description = "Object name")] string ObjectName,
            [ExcelArgument(Description = "Asset Id")] string AssetId,
            [ExcelArgument(Description = "Fixings array, 1st column dates / 2nd column fixings")] object[,] Fixings)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var dict = new FixingDictionary
                {
                    Name = AssetId
                };
                var dictData = Fixings.RangeToDictionary<DateTime, double>();
                foreach (var kv in dictData)
                    dict.Add(kv.Key, kv.Value);

                var cache = ContainerStores.GetObjectCache<IFixingDictionary>();
                cache.PutObject(ObjectName, new SessionItem<IFixingDictionary> { Name = ObjectName, Value = dict });
                return ObjectName + '¬' + cache.GetObject(ObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Creates a fixing dictionary", Category = CategoryNames.Curves, Name = CategoryNames.Curves + "_" + nameof(CreateFixingDictionaryFromVectors))]
        public static object CreateFixingDictionaryFromVectors(
            [ExcelArgument(Description = "Object name")] string ObjectName,
            [ExcelArgument(Description = "Asset Id")] string AssetId,
            [ExcelArgument(Description = "Fixings dates")] double[] FixingDates, 
            [ExcelArgument(Description = "Fixings dates")] double[] Fixings)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                if (FixingDates.Length != Fixings.Length)
                    throw new Exception("Fixings and FixingDates must be of same length");

                var dict = new FixingDictionary
                {
                    Name = AssetId
                };
                for(var i=0;i<FixingDates.Length;i++)
                    dict.Add(DateTime.FromOADate(FixingDates[i]), Fixings[i]);

                var cache = ContainerStores.GetObjectCache<IFixingDictionary>();
                cache.PutObject(ObjectName, new SessionItem<IFixingDictionary> { Name = ObjectName, Value = dict });
                return ObjectName + '¬' + cache.GetObject(ObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Creates a correlation matrix", Category = CategoryNames.Curves, Name = CategoryNames.Curves + "_" + nameof(CreateCorrelationMatrix))]
        public static object CreateCorrelationMatrix(
            [ExcelArgument(Description = "Object name")] string ObjectName,
            [ExcelArgument(Description = "Labels X")] object[] LabelsX,
            [ExcelArgument(Description = "Labels Y")] object[] LabelsY,
            [ExcelArgument(Description = "Correlations")] double[,] Correlations)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var matrix = new CorrelationMatrix(LabelsX.ObjectRangeToVector<string>(), LabelsY.ObjectRangeToVector<string>(), Correlations);

                var cache = ContainerStores.GetObjectCache<ICorrelationMatrix>();
                cache.PutObject(ObjectName, new SessionItem<ICorrelationMatrix> { Name = ObjectName, Value = matrix });
                return ObjectName + '¬' + cache.GetObject(ObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Bends a spread curve to a sparse set of updated spreads", Category = CategoryNames.Curves, Name = CategoryNames.Curves + "_" + nameof(BendCurve))]
        public static object BendCurve(
            [ExcelArgument(Description = "Input spreads")] double[] InputSpreads,
            [ExcelArgument(Description = "Sparse new spreads")] object[] SparseNewSpreads)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var sparseSpreads = SparseNewSpreads.Select(x => x is ExcelEmpty ? null : (double?)x).ToArray();
                var o = Math.CurveBender.Bend(InputSpreads, sparseSpreads);
                return o.ReturnExcelRangeVectorFromDouble();
            });
        }
    }
}
