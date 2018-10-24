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
using Qwack.Futures;
using Qwack.Dates;
using Qwack.Math;

namespace Qwack.Excel.Curves
{
    public class PriceCurveFunctions
    {
        private static readonly ILogger _logger = ContainerStores.GlobalContainer.GetService<ILoggerFactory>()?.CreateLogger<PriceCurveFunctions>();

        [ExcelFunction(Description = "Creates a price curve", Category = CategoryNames.Curves, Name = CategoryNames.Curves + "_" + nameof(CreatePriceCurve))]
        public static object CreatePriceCurve(
             [ExcelArgument(Description = "Object name")] string ObjectName,
             [ExcelArgument(Description = "Asset Id")] string AssetId,
             [ExcelArgument(Description = "Build date")] DateTime BuildDate,
             [ExcelArgument(Description = "Array of pillar dates")] double[] Pillars,
             [ExcelArgument(Description = "Array of prices values")] double[] Prices,
             [ExcelArgument(Description = "Type of curve, e.g. LME, ICE, NYMEX etc")] object CurveType,
             [ExcelArgument(Description = "Array of pillar labels (optional)")] object PillarLabels,
             [ExcelArgument(Description = "Currency - default USD")] object Currency,
             [ExcelArgument(Description = "Collateral spec, required for delta calculation - default LIBOR.3M")] object CollateralSpec,
             [ExcelArgument(Description = "Spot lag, required for theta, default 0b")] object SpotLag,
             [ExcelArgument(Description = "Spot calendar, required for theta, default USD")] object SpotCalendar)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var curveTypeStr = CurveType.OptionalExcel("Linear");
                var ccy = Currency.OptionalExcel("USD");
                var colSpec = CollateralSpec.OptionalExcel("LIBOR.3M");
                var spotLagStr = SpotLag.OptionalExcel("0b");
                var spotCalStr = SpotCalendar.OptionalExcel("USD");

                if (!Enum.TryParse(curveTypeStr, out PriceCurveType cType))
                {
                    return $"Could not parse price curve type - {curveTypeStr}";
                }

                ContainerStores.SessionContainer.GetService<ICalendarProvider>().Collection.TryGetCalendar(ccy, out var ccyCal);
                ContainerStores.SessionContainer.GetService<ICalendarProvider>().Collection.TryGetCalendar(spotCalStr, out var spotCal);
                var ccyObj = ContainerStores.GlobalContainer.GetRequiredService<ICurrencyProvider>()[ccy];

                var labels = (PillarLabels is ExcelMissing) ? null : ((object[,])PillarLabels).ObjectRangeToVector<string>();

                var pDates = Pillars.ToDateTimeArray();
                var cObj = new PriceCurve(BuildDate, pDates, Prices, cType, ContainerStores.GlobalContainer.GetRequiredService<ICurrencyProvider>(), labels)
                {
                    Name = AssetId ?? ObjectName,
                    AssetId = AssetId ?? ObjectName,
                    Currency = ccyObj,
                    CollateralSpec = colSpec,
                    SpotCalendar = spotCal,
                    SpotLag = new Frequency(spotLagStr)
                };

                var cache = ContainerStores.GetObjectCache<IPriceCurve>();
                cache.PutObject(ObjectName, new SessionItem<IPriceCurve> { Name = ObjectName, Value = cObj });
                return ObjectName + '¬' + cache.GetObject(ObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Creates a contango price curve for precious metals", Category = CategoryNames.Curves, Name = CategoryNames.Curves + "_" + nameof(CreateContangoPriceCurve))]
        public static object CreateContangoPriceCurve(
            [ExcelArgument(Description = "Object name")] string ObjectName,
            [ExcelArgument(Description = "Asset Id")] string AssetId,
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
                var cObj = new ContangoPriceCurve(BuildDate, SpotPrice, SpotDate, pDates, ContangoRates, ContainerStores.GlobalContainer.GetRequiredService<ICurrencyProvider>(), Qwack.Dates.DayCountBasis.Act360, labels)
                {
                    Name = AssetId ?? ObjectName,
                    AssetId = AssetId ?? ObjectName
                };
                var cache = ContainerStores.GetObjectCache<IPriceCurve>();
                cache.PutObject(ObjectName, new SessionItem<IPriceCurve> { Name = ObjectName, Value = cObj });
                return ObjectName + '¬' + cache.GetObject(ObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Creates a sparse price curve", Category = CategoryNames.Curves, Name = CategoryNames.Curves + "_" + nameof(CreateSparsePriceCurve))]
        public static object CreateSparsePriceCurve(
            [ExcelArgument(Description = "Object name")] string ObjectName,
            [ExcelArgument(Description = "Asset Id")] string AssetId,
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
                var cObj = new SparsePriceCurve(BuildDate, pDates, Prices, cType, ContainerStores.GlobalContainer.GetRequiredService<ICurrencyProvider>())
                {
                    Name = AssetId ?? ObjectName,
                    AssetId = AssetId ?? ObjectName
                };
                var cache = ContainerStores.GetObjectCache<IPriceCurve>();
                cache.PutObject(ObjectName, new SessionItem<IPriceCurve> { Name = ObjectName, Value = cObj });
                return ObjectName + '¬' + cache.GetObject(ObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Creates a sparse price curve from swap objects", Category = CategoryNames.Curves, Name = CategoryNames.Curves + "_" + nameof(CreateSparsePriceCurveFromSwaps))]
        public static object CreateSparsePriceCurveFromSwaps(
            [ExcelArgument(Description = "Object name")] string ObjectName,
            [ExcelArgument(Description = "Asset Id")] string AssetId,
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

                var irCache = ContainerStores.GetObjectCache<IIrCurve>();
                if (!irCache.TryGetObject(DiscountCurveName, out var irCurveObj))
                {
                    return $"Could not find ir curve with name {DiscountCurveName}";
                }
                var irCurve = irCurveObj.Value;

                var swapCache = ContainerStores.GetObjectCache<AsianSwapStrip>();
                var swaps = Swaps.Select(s => swapCache.GetObject(s as string)).Select(x => x.Value);

                var pDates = Pillars.ToDateTimeArray();
                var fitter = new Core.Calibrators.NewtonRaphsonAssetCurveSolver();
                var cObj = (SparsePriceCurve)fitter.Solve(swaps.ToList(), pDates.ToList(), irCurve, BuildDate, ContainerStores.GlobalContainer.GetRequiredService<ICurrencyProvider>());
                cObj.Name = AssetId ?? ObjectName;
                cObj.AssetId = AssetId ?? ObjectName;

                var cache = ContainerStores.GetObjectCache<IPriceCurve>();
                cache.PutObject(ObjectName, new SessionItem<IPriceCurve> { Name = ObjectName, Value = cObj });
                return ObjectName + '¬' + cache.GetObject(ObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Creates a price curve from basis swap objects", Category = CategoryNames.Curves, Name = CategoryNames.Curves + "_" + nameof(CreatePriceCurveFromBasisSwaps))]
        public static object CreatePriceCurveFromBasisSwaps(
          [ExcelArgument(Description = "Object name")] string ObjectName,
          [ExcelArgument(Description = "Asset Id")] string AssetId,
          [ExcelArgument(Description = "Base curve object")] string BaseCurve,
          [ExcelArgument(Description = "Build date")] DateTime BuildDate,
          [ExcelArgument(Description = "Array of pillar dates")] double[] Pillars,
          [ExcelArgument(Description = "Array of swap objects")] object[] Swaps,
          [ExcelArgument(Description = "Discount curve name")] string DiscountCurveName,
          [ExcelArgument(Description = "Type of curve, e.g. LME, ICE, NYMEX etc")] object CurveType)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var curveTypeStr = CurveType.OptionalExcel<string>("Coal");
                if (!Enum.TryParse(curveTypeStr, out PriceCurveType cType))
                {
                    return $"Could not parse price curve type - {curveTypeStr}";
                }

                var irCache = ContainerStores.GetObjectCache<IIrCurve>();
                if(!irCache.TryGetObject(DiscountCurveName, out var irCurveObj))
                {
                    return $"Could not find ir curve with name {DiscountCurveName}";
                }
                var irCurve = irCurveObj.Value;

                var curveCache = ContainerStores.GetObjectCache<IPriceCurve>();
                if (!curveCache.TryGetObject(BaseCurve, out var bCurveObj))
                {
                    return $"Could not find ir curve with name {DiscountCurveName}";
                }
                var baseCurve = bCurveObj.Value;

                var swapCache = ContainerStores.GetObjectCache<AsianBasisSwap>();
                var swaps = Swaps.Select(s => swapCache.GetObject(s as string)).Select(x => x.Value);

                var pDates = Pillars.ToDateTimeArray();
                var fitter = new Core.Calibrators.NewtonRaphsonAssetBasisCurveSolver(ContainerStores.CurrencyProvider);
                var cObj = (PriceCurve)fitter.SolveCurve(swaps.ToList(), pDates.ToList(), irCurve, baseCurve, BuildDate, cType);
                cObj.Name = AssetId ?? ObjectName;
                cObj.AssetId = AssetId ?? ObjectName;

                curveCache.PutObject(ObjectName, new SessionItem<IPriceCurve> { Name = ObjectName, Value = cObj });
                return ObjectName + '¬' + curveCache.GetObject(ObjectName).Version;
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
            [ExcelArgument(Description = "Fixings array, 1st column dates / 2nd column fixings")] object[,] Fixings,
            [ExcelArgument(Description = "Type, Asset or FX - default Asset")] object FixingType,
            [ExcelArgument(Description = "Fx pair (optional)")] string FxPair)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var fixTypeStr = FixingType.OptionalExcel("Asset");
                if(!Enum.TryParse<FixingDictionaryType>(fixTypeStr, out var fixType))
                {
                    throw new Exception($"Unknown fixing dictionary type {fixTypeStr}");
                }
                var dict = new FixingDictionary
                {
                    Name = AssetId,
                    AssetId = AssetId,
                    FixingDictionaryType = fixType,
                    FxPair = FxPair
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
            [ExcelArgument(Description = "Fixings dates")] double[] Fixings,
            [ExcelArgument(Description = "Type, Asset or FX - default Asset")] object FixingType,
            [ExcelArgument(Description = "Fx pair (optional)")] string FxPair)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                if (FixingDates.Length != Fixings.Length)
                    throw new Exception("Fixings and FixingDates must be of same length");

                var fixTypeStr = FixingType.OptionalExcel("Asset");
                if (!Enum.TryParse<FixingDictionaryType>(fixTypeStr, out var fixType))
                {
                    throw new Exception($"Unknown fixing dictionary type {fixTypeStr}");
                }

                var dict = new FixingDictionary
                {
                    Name = AssetId,
                    AssetId = AssetId,
                    FixingDictionaryType = fixType,
                    FxPair = FxPair
                };
                for (var i = 0; i < FixingDates.Length; i++)
                    if (FixingDates[i] != 0)
                        dict.Add(DateTime.FromOADate(FixingDates[i]), Fixings[i]);

                var cache = ContainerStores.GetObjectCache<IFixingDictionary>();
                cache.PutObject(ObjectName, new SessionItem<IFixingDictionary> { Name = ObjectName, Value = dict });
                return ObjectName + '¬' + cache.GetObject(ObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Creates a fixing dictionary for a rolling futures index", Category = CategoryNames.Curves, Name = CategoryNames.Curves + "_" + nameof(CreateFixingDictionaryForRollingFuture))]
        public static object CreateFixingDictionaryForRollingFuture(
            [ExcelArgument(Description = "Object name")] string ObjectName,
            [ExcelArgument(Description = "Asset Id")] string AssetId,
            [ExcelArgument(Description = "Futures code, e.g. QS or CO")] string FuturesCode,
            [ExcelArgument(Description = "1st month fixings array, 1st column dates / 2nd column fixings")] object[,] Fixings1m,
            [ExcelArgument(Description = "2nd month fixings array, 1st column dates / 2nd column fixings")] object[,] Fixings2m)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var dict = new FixingDictionary
                {
                    Name = AssetId,
                    AssetId = AssetId,
                    FixingDictionaryType = FixingDictionaryType.Asset
                };
                var futuresProvider = ContainerStores.SessionContainer.GetRequiredService<IFutureSettingsProvider>();
                var dictData1m = Fixings1m.RangeToDictionary<DateTime, double>();
                var dictData2m = Fixings2m.RangeToDictionary<DateTime, double>();
                var fc = new FutureCode(FuturesCode, futuresProvider)
                {
                    YearBeforeWhich2DigitDatesAreUsed = DateTime.Today.Year - 2
                };

                foreach (var kv in dictData1m)
                {
                    var currentFM = fc.GetFrontMonth(kv.Key, true);
                    var cfm = new FutureCode(currentFM, DateTime.Today.Year - 2, futuresProvider);
                    if(kv.Key<=cfm.GetRollDate())
                        dict.Add(kv.Key, kv.Value);
                    else
                        dict.Add(kv.Key, dictData2m[kv.Key]);
                }

                var cache = ContainerStores.GetObjectCache<IFixingDictionary>();
                cache.PutObject(ObjectName, new SessionItem<IFixingDictionary> { Name = ObjectName, Value = dict });
                return ObjectName + '¬' + cache.GetObject(ObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Display contents of a fixing dictionary", Category = CategoryNames.Curves, Name = CategoryNames.Curves + "_" + nameof(DisplayFixingDictionary))]
        public static object DisplayFixingDictionary(
            [ExcelArgument(Description = "Object name")] string ObjectName,
            [ExcelArgument(Description = "Optional - fixing date")] object FixingDate,
            [ExcelArgument(Description = "Reverse sort order, False (default) or True")] bool ReverseSort)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var dict = ContainerStores.GetObjectCache<IFixingDictionary>().GetObjectOrThrow(ObjectName, $"Fixing dictionary not found with name {ObjectName}");

                if (FixingDate is ExcelMissing)
                {
                    var o = new object[dict.Value.Count, 2];
                    var c = 0;

                    if (ReverseSort)
                        foreach (var kv in dict.Value.OrderByDescending(x=>x.Key))
                        {
                            o[c, 0] = kv.Key;
                            o[c, 1] = kv.Value;
                            c++;
                        }
                    else
                        foreach (var kv in dict.Value)
                        {
                            o[c, 0] = kv.Key;
                            o[c, 1] = kv.Value;
                            c++;
                        }

                    return o;
                }
                else
                {
                    var d = FixingDate as double?;
                    if (!d.HasValue || !dict.Value.TryGetValue(DateTime.FromOADate(d.Value), out var fixing))
                        throw new Exception($"Fixing not found for date {FixingDate}");

                    return fixing;
                }
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
                var sparseSpreads = SparseNewSpreads
                    .Select(x => x is ExcelEmpty || !(x is double) ? 
                    null : (double?)x).ToArray();
                var o = CurveBender.Bend(InputSpreads, sparseSpreads);
                return o.ReturnExcelRangeVectorFromDouble();
            });
        }
    }
}
