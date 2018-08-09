using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExcelDna.Integration;
using Qwack.Core.Curves;
using Qwack.Core.Basic;
using Qwack.Excel.Services;
using Qwack.Excel.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Qwack.Dates;
using Qwack.Core.Instruments.Asset;
using Qwack.Core.Instruments.Funding;

namespace Qwack.Excel.Curves
{
    public class FundingInstrumentFunctions
    {
        private static readonly ILogger _logger = ContainerStores.GlobalContainer.GetService<ILoggerFactory>()?.CreateLogger<FundingInstrumentFunctions>();

        
        [ExcelFunction(Description = "Creates a FRA object", Category = CategoryNames.Instruments, Name = CategoryNames.Instruments + "_" + nameof(CreateFRA))]
        public static object CreateFRA(
             [ExcelArgument(Description = "Object name")] string ObjectName,
             [ExcelArgument(Description = "Value date")] DateTime ValDate,
             [ExcelArgument(Description = "FRA code e.g. 3X6")] string PeriodCode,
             [ExcelArgument(Description = "Rate Index")] string RateIndex,
             [ExcelArgument(Description = "Currency")] string Currency,
             [ExcelArgument(Description = "Par Rate")] double ParRate,
             [ExcelArgument(Description = "Notional")] double Notional,
             [ExcelArgument(Description = "Forecast Curve")] string ForecastCurve,
             [ExcelArgument(Description = "Discount Curve")] string DiscountCurve,
             [ExcelArgument(Description = "DiscountingType")] object DiscountingType,
             [ExcelArgument(Description = "Pay / Receive")] object PayRec)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var discType = DiscountingType.OptionalExcel("Isda");
                var payRec = PayRec.OptionalExcel("Pay");
           
                if (!ContainerStores.GetObjectCache<FloatRateIndex>().TryGetObject(RateIndex, out var rIndex))
                {
                    _logger?.LogInformation("Rate index {index} not found in cache", RateIndex);
                    return $"Rate index {RateIndex} not found in cache";
                }

                if (!Enum.TryParse(payRec, out SwapPayReceiveType pType))
                {
                    return $"Could not parse pay/rec - {payRec}";
                }

                if (!Enum.TryParse(discType, out FraDiscountingType fType))
                {
                    return $"Could not parse FRA discounting type - {discType}";
                }

                var product = new ForwardRateAgreement(ValDate, PeriodCode, ParRate, rIndex.Value, pType, fType, ForecastCurve, DiscountCurve);

                var cache = ContainerStores.GetObjectCache<ForwardRateAgreement>();
                cache.PutObject(ObjectName, new SessionItem<ForwardRateAgreement> { Name = ObjectName, Value = product });
                return ObjectName + '¬' + cache.GetObject(ObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Creates an fx forward object", Category = CategoryNames.Instruments, Name = CategoryNames.Instruments + "_" + nameof(CreateFxForward))]
        public static object CreateFxForward(
           [ExcelArgument(Description = "Object name")] string ObjectName,
           [ExcelArgument(Description = "Settle Date")] DateTime SettleDate,
           [ExcelArgument(Description = "Domestic Currency")] string DomesticCcy,
           [ExcelArgument(Description = "Foreign Currency")] string ForeignCcy,
           [ExcelArgument(Description = "Domestic Notional")] double DomesticNotional,
           [ExcelArgument(Description = "Strike")] double Strike,
           [ExcelArgument(Description = "Foreign Discount Curve")] string DiscountCurve,
           [ExcelArgument(Description = "Solve Curve")] string SolveCurve)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
              
                var product = new FxForward
                {
                    DomesticCCY = new Currency(DomesticCcy,DayCountBasis.Act365F,null),
                    ForeignCCY = new Currency(ForeignCcy, DayCountBasis.Act365F, null),
                    DomesticQuantity = DomesticNotional,
                    DeliveryDate = SettleDate,
                    ForeignDiscountCurve = DiscountCurve,
                    SolveCurve = SolveCurve,
                    Strike = Strike
                };

                var cache = ContainerStores.GetObjectCache<FxForward>();
                cache.PutObject(ObjectName, new SessionItem<FxForward> { Name = ObjectName, Value = product });
                return ObjectName + '¬' + cache.GetObject(ObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Creates a standard interest rate swap object following conventions for the given rate index", Category = CategoryNames.Instruments, Name = CategoryNames.Instruments + "_" + nameof(CreateIRS))]
        public static object CreateIRS(
               [ExcelArgument(Description = "Object name")] string ObjectName,
               [ExcelArgument(Description = "Value date")] DateTime ValDate,
               [ExcelArgument(Description = "Tenor")] string SwapTenor,
               [ExcelArgument(Description = "Rate Index")] string RateIndex,
               [ExcelArgument(Description = "Par Rate")] double ParRate,
               [ExcelArgument(Description = "Notional")] double Notional,
               [ExcelArgument(Description = "Forecast Curve")] string ForecastCurve,
               [ExcelArgument(Description = "Discount Curve")] string DiscountCurve,
               [ExcelArgument(Description = "Pay / Receive")] object PayRec)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var payRec = PayRec.OptionalExcel("Pay");

                if (!ContainerStores.GetObjectCache<FloatRateIndex>().TryGetObject(RateIndex, out var rIndex))
                {
                    _logger?.LogInformation("Rate index {index} not found in cache", RateIndex);
                    return $"Rate index {RateIndex} not found in cache";
                }

                if (!Enum.TryParse(payRec, out SwapPayReceiveType pType))
                {
                    return $"Could not parse pay/rec - {payRec}";
                }

                var tenor = new Frequency(SwapTenor);

                var product = new IrSwap(ValDate, tenor, rIndex.Value, ParRate, pType, ForecastCurve, DiscountCurve);

                var cache = ContainerStores.GetObjectCache<IrSwap>();
                cache.PutObject(ObjectName, new SessionItem<IrSwap> { Name = ObjectName, Value = product });
                return ObjectName + '¬' + cache.GetObject(ObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Creates a collection of funding instruments to calibrate a curve engine", Category = CategoryNames.Instruments, Name = CategoryNames.Instruments + "_" + nameof(CreateFundingInstrumentCollection))]
        public static object CreateFundingInstrumentCollection(
           [ExcelArgument(Description = "Object name")] string ObjectName,
           [ExcelArgument(Description = "Value date")] object[] Instruments)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var swapCache = ContainerStores.GetObjectCache<IrSwap>();
                var swaps = Instruments
                    .Where(s => swapCache.Exists(s as string))
                    .Select(s => swapCache.GetObject(s as string).Value);

                var fraCache = ContainerStores.GetObjectCache<ForwardRateAgreement>();
                var fras = Instruments
                    .Where(s => fraCache.Exists(s as string))
                    .Select(s => fraCache.GetObject(s as string).Value);

                var stirCache = ContainerStores.GetObjectCache<STIRFuture>();
                var futures = Instruments
                    .Where(s => stirCache.Exists(s as string))
                    .Select(s => stirCache.GetObject(s as string).Value);

                var fxFwdCache = ContainerStores.GetObjectCache<FxForward>();
                var fxFwds = Instruments
                    .Where(s => fxFwdCache.Exists(s as string))
                    .Select(s => fxFwdCache.GetObject(s as string).Value);

                var xccySwapCache = ContainerStores.GetObjectCache<XccyBasisSwap>();
                var xccySwaps = Instruments
                    .Where(s => xccySwapCache.Exists(s as string))
                    .Select(s => xccySwapCache.GetObject(s as string).Value);

                var fic = new FundingInstrumentCollection();
                fic.AddRange(swaps);
                fic.AddRange(fras);
                fic.AddRange(futures);
                fic.AddRange(fxFwds);
                fic.AddRange(xccySwaps);

                var cache = ContainerStores.GetObjectCache<FundingInstrumentCollection>();
                cache.PutObject(ObjectName, new SessionItem<FundingInstrumentCollection> { Name = ObjectName, Value = fic });
                return ObjectName + '¬' + cache.GetObject(ObjectName).Version;
            });
        }


        [ExcelFunction(Description = "Creates a new rate index object", Category = CategoryNames.Instruments, Name = CategoryNames.Instruments + "_" + nameof(CreateRateIndex))]
        public static object CreateRateIndex(
              [ExcelArgument(Description = "Index name")] string IndexName,
              [ExcelArgument(Description = "Currency")] string Currency,
              [ExcelArgument(Description = "Rate forecast tenor")] string ForecastTenor,
              [ExcelArgument(Description = "Day count basis, float leg")] string DaycountBasisFloat,
              [ExcelArgument(Description = "Day count basis, fixed leg")] string DaycountBasisFixed,
              [ExcelArgument(Description = "Fixed leg reset tenor")] string FixedTenor,
              [ExcelArgument(Description = "Holiday calendars")] string HolidayCalendars,
              [ExcelArgument(Description = "Fixing offset, e.g. 2b")] string FixingOffset,
              [ExcelArgument(Description = "Roll convention")] string RollConvention)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
               
                if (!Enum.TryParse(DaycountBasisFixed, out DayCountBasis dFixed))
                {
                    return $"Could not parse fixed daycount - {DaycountBasisFixed}";
                }
                if (!Enum.TryParse(DaycountBasisFloat, out DayCountBasis dFloat))
                {
                    return $"Could not parse float daycount - {DaycountBasisFloat}";
                }
                if (!Enum.TryParse(RollConvention, out RollType rConv))
                {
                    return $"Could not parse roll convention - {RollConvention}";
                }

                var floatTenor = new Frequency(ForecastTenor);
                var fixedTenor = new Frequency(FixedTenor);
                var fixOffset = new Frequency(FixingOffset);


                if (!ContainerStores.SessionContainer.GetService<ICalendarProvider>().Collection.TryGetCalendar(HolidayCalendars, out var cal))
                {
                    _logger?.LogInformation("Calendar {HolidayCalendars} not found in cache", HolidayCalendars);
                    return $"Calendar {HolidayCalendars} not found in cache";
                }

                var rIndex = new FloatRateIndex
                {
                    Currency = new Currency(Currency, DayCountBasis.Act365F, null),
                    RollConvention = rConv,
                    FixingOffset = fixOffset,
                    ResetTenor = floatTenor,
                    ResetTenorFixed = fixedTenor,
                    HolidayCalendars = cal,
                    DayCountBasis = dFloat,
                    DayCountBasisFixed = dFixed
                };
               
                var cache = ContainerStores.GetObjectCache<FloatRateIndex>();
                cache.PutObject(IndexName, new SessionItem<FloatRateIndex> { Name = IndexName, Value = rIndex });
                return IndexName + '¬' + cache.GetObject(IndexName).Version;
            });
        }
    }
}
