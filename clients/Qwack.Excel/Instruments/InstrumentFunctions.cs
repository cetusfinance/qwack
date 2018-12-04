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
using Qwack.Core.Instruments;
using Qwack.Core.Models;
using Qwack.Models.Models;
using Qwack.Core.Cubes;
using Qwack.Models.Risk;
using Qwack.Models.MCModels;

namespace Qwack.Excel.Instruments
{
    public class InstrumentFunctions
    {
        private static readonly ILogger _logger = ContainerStores.GlobalContainer.GetService<ILoggerFactory>()?.CreateLogger<InstrumentFunctions>();

        [ExcelFunction(Description = "Creates an asian swap, term settled / single period", Category = CategoryNames.Instruments, Name = CategoryNames.Instruments + "_" + nameof(CreateAsianSwap))]
        public static object CreateAsianSwap(
            [ExcelArgument(Description = "Object name")] string ObjectName,
            [ExcelArgument(Description = "Period code or dates")] object PeriodCodeOrDates,
            [ExcelArgument(Description = "Asset Id")] string AssetId,
            [ExcelArgument(Description = "Currency")] string Currency,
            [ExcelArgument(Description = "Strike")] double Strike,
            [ExcelArgument(Description = "Notional")] double Notional,
            [ExcelArgument(Description = "Fixing calendar")] object FixingCalendar,
            [ExcelArgument(Description = "Payment calendar")] object PaymentCalendar,
            [ExcelArgument(Description = "Payment offset or date")] object PaymentOffsetOrDate,
            [ExcelArgument(Description = "Spot lag")] object SpotLag,
            [ExcelArgument(Description = "Fixing date generation type")] object DateGenerationType,
            [ExcelArgument(Description = "Discount curve")] string DiscountCurve)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var fixingCal = FixingCalendar.OptionalExcel("WeekendsOnly");
                var paymentCal = PaymentCalendar.OptionalExcel("WeekendsOnly");
                var spotLag = SpotLag.OptionalExcel("0b");
                var dGenType = DateGenerationType.OptionalExcel("BusinessDays");
                var paymentOffset = PaymentOffsetOrDate is double ? "0b" : PaymentOffsetOrDate.OptionalExcel("0b");

                if (!ContainerStores.SessionContainer.GetService<ICalendarProvider>().Collection.TryGetCalendar(fixingCal, out var fCal))
                {
                    _logger?.LogInformation("Calendar {calendar} not found in cache", fixingCal);
                    return $"Calendar {fixingCal} not found in cache";
                }
                if (!ContainerStores.SessionContainer.GetService<ICalendarProvider>().Collection.TryGetCalendar(paymentCal, out var pCal))
                {
                    _logger?.LogInformation("Calendar {calendar} not found in cache", paymentCal);
                    return $"Calendar {paymentCal} not found in cache";
                }
                var pOffset = new Frequency(paymentOffset);
                var sLag = new Frequency(spotLag);
                if (!Enum.TryParse(dGenType, out DateGenerationType dType))
                {
                    return $"Could not parse date generation type - {dGenType}";
                }
                var currency = ContainerStores.GlobalContainer.GetRequiredService<ICurrencyProvider>()[Currency];

                AsianSwap product;
                if (PeriodCodeOrDates is object[,])
                {
                    var dates = ((object[,])PeriodCodeOrDates).ObjectRangeToVector<double>().ToDateTimeArray();
                    if (PaymentOffsetOrDate is double)
                        product = AssetProductFactory.CreateTermAsianSwap(dates[0], dates[1], Strike, AssetId, fCal, DateTime.FromOADate((double)PaymentOffsetOrDate), currency, TradeDirection.Long, sLag, Notional, dType);
                    else
                        product = AssetProductFactory.CreateTermAsianSwap(dates[0], dates[1], Strike, AssetId, fCal, pCal, pOffset, currency, TradeDirection.Long, sLag, Notional, dType);
                }
                else if (PeriodCodeOrDates is double)
                {
                    PeriodCodeOrDates = DateTime.FromOADate((double)PeriodCodeOrDates).ToString("MMM-yy");
                    product = AssetProductFactory.CreateTermAsianSwap(PeriodCodeOrDates as string, Strike, AssetId, fCal, pCal, pOffset, currency, TradeDirection.Long, sLag, Notional, dType);
                }
                else
                    product = AssetProductFactory.CreateTermAsianSwap(PeriodCodeOrDates as string, Strike, AssetId, fCal, pCal, pOffset, currency, TradeDirection.Long, sLag, Notional, dType);

                product.TradeId = ObjectName;
                product.DiscountCurve = DiscountCurve;
                
                var cache = ContainerStores.GetObjectCache<AsianSwap>();
                cache.PutObject(ObjectName, new SessionItem<AsianSwap> { Name = ObjectName, Value = product });
                return ObjectName + '¬' + cache.GetObject(ObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Creates an asian crack/diff swap, term settled / single period", Category = CategoryNames.Instruments, Name = CategoryNames.Instruments + "_" + nameof(CreateAsianCrackDiffSwap))]
        public static object CreateAsianCrackDiffSwap(
           [ExcelArgument(Description = "Object name")] string ObjectName,
           [ExcelArgument(Description = "Period code")] object PeriodCode,
           [ExcelArgument(Description = "Asset Id pay")] string AssetIdPay,
           [ExcelArgument(Description = "Asset Id receive")] string AssetIdRec,
           [ExcelArgument(Description = "Currency")] string Currency,
           [ExcelArgument(Description = "Strike")] double Strike,
           [ExcelArgument(Description = "Notional pay")] double NotionalPay,
           [ExcelArgument(Description = "Notional receive")] double NotionalRec,
           [ExcelArgument(Description = "Fixing calendar pay")] object FixingCalendarPay,
           [ExcelArgument(Description = "Fixing calendar receive")] object FixingCalendarRec,
           [ExcelArgument(Description = "Payment calendar")] object PaymentCalendar,
           [ExcelArgument(Description = "Payment offset")] object PaymentOffset,
           [ExcelArgument(Description = "Spot lag pay")] object SpotLagPay,
           [ExcelArgument(Description = "Spot lag receive")] object SpotLagRec,
           [ExcelArgument(Description = "Discount curve")] string DiscountCurve)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var fixingCalPay = FixingCalendarPay.OptionalExcel("WeekendsOnly");
                var fixingCalRec = FixingCalendarRec.OptionalExcel("WeekendsOnly");
                var paymentCal = PaymentCalendar.OptionalExcel("WeekendsOnly");
                var spotLagPay = SpotLagPay.OptionalExcel("0b");
                var spotLagRec = SpotLagRec.OptionalExcel("0b");

                var paymentOffset = PaymentOffset.OptionalExcel("0b");

                if (!ContainerStores.SessionContainer.GetService<ICalendarProvider>().Collection.TryGetCalendar(fixingCalPay, out var fCalPay))
                {
                    _logger?.LogInformation("Calendar {calendar} not found in cache", fixingCalPay);
                    return $"Calendar {fixingCalPay} not found in cache";
                }
                if (!ContainerStores.SessionContainer.GetService<ICalendarProvider>().Collection.TryGetCalendar(fixingCalRec, out var fCalRec))
                {
                    _logger?.LogInformation("Calendar {calendar} not found in cache", fixingCalRec);
                    return $"Calendar {fixingCalRec} not found in cache";
                }
                if (!ContainerStores.SessionContainer.GetService<ICalendarProvider>().Collection.TryGetCalendar(paymentCal, out var pCal))
                {
                    _logger?.LogInformation("Calendar {calendar} not found in cache", paymentCal);
                    return $"Calendar {paymentCal} not found in cache";
                }

                var pOffset = new Frequency(paymentOffset);
                var sLagPay = new Frequency(spotLagPay);
                var sLagRec = new Frequency(spotLagRec);

                var currency = ContainerStores.GlobalContainer.GetRequiredService<ICurrencyProvider>()[Currency];

                AsianBasisSwap product;
                if (PeriodCode is double)
                {
                    PeriodCode = DateTime.FromOADate((double)PeriodCode).ToString("MMM-yy");
                    
                }

                product = AssetProductFactory.CreateTermAsianBasisSwap(PeriodCode as string, Strike, AssetIdPay, AssetIdRec, fCalPay, fCalRec, pCal, pOffset, currency, sLagPay, sLagRec, NotionalPay, NotionalRec);

                product.TradeId = ObjectName;
                foreach (var ps in product.PaySwaplets)
                    ps.DiscountCurve = DiscountCurve;
                foreach (var rs in product.RecSwaplets)
                    rs.DiscountCurve = DiscountCurve;

                var cache = ContainerStores.GetObjectCache<AsianBasisSwap>();
                cache.PutObject(ObjectName, new SessionItem<AsianBasisSwap> { Name = ObjectName, Value = product });
                return ObjectName + '¬' + cache.GetObject(ObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Creates a commodity future position", Category = CategoryNames.Instruments, Name = CategoryNames.Instruments + "_" + nameof(CreateFuture))]
        public static object CreateFuture(
            [ExcelArgument(Description = "Object name")] string ObjectName,
            [ExcelArgument(Description = "Expiry date")] DateTime ExpiryDate,
            [ExcelArgument(Description = "Asset Id")] string AssetId,
            [ExcelArgument(Description = "Currency")] string Currency,
            [ExcelArgument(Description = "Strike")] double Strike,
            [ExcelArgument(Description = "Quantity of contracts")] double Quantity,
            [ExcelArgument(Description = "Contract lot size")] double LotSize,
            [ExcelArgument(Description = "Price multiplier - default 1.0")] object PriceMultiplier)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var multiplier = PriceMultiplier.OptionalExcel(1.0);
                var currency = ContainerStores.GlobalContainer.GetRequiredService<ICurrencyProvider>()[Currency];

                var product = new Future
                {
                    AssetId = AssetId,
                     ContractQuantity = Quantity,
                     LotSize = LotSize,
                     PriceMultiplier = multiplier,
                     Currency = currency,
                     Strike = Strike,
                     Direction = TradeDirection.Long,
                     ExpiryDate = ExpiryDate,
                     TradeId = ObjectName
                };
                
                var cache = ContainerStores.GetObjectCache<Future>();
                cache.PutObject(ObjectName, new SessionItem<Future> { Name = ObjectName, Value = product });
                return ObjectName + '¬' + cache.GetObject(ObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Creates a commodity futures option position", Category = CategoryNames.Instruments, Name = CategoryNames.Instruments + "_" + nameof(CreateFutureOption))]
        public static object CreateFutureOption(
          [ExcelArgument(Description = "Object name")] string ObjectName,
          [ExcelArgument(Description = "Expiry date")] DateTime ExpiryDate,
          [ExcelArgument(Description = "Asset Id")] string AssetId,
          [ExcelArgument(Description = "Currency")] string Currency,
          [ExcelArgument(Description = "Strike")] double Strike,
          [ExcelArgument(Description = "Quantity of contracts")] double Quantity,
          [ExcelArgument(Description = "Contract lot size")] double LotSize,
          [ExcelArgument(Description = "Call or Put flag")] string CallPut,
          [ExcelArgument(Description = "Exercise style - default European")] object ExerciseStyle,
          [ExcelArgument(Description = "Margining type, FuturesStyle or Regular - default FuturesStyle")] object MarginType)

        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var currency = ContainerStores.GlobalContainer.GetRequiredService<ICurrencyProvider>()[Currency];
                var exStyle = ExerciseStyle.OptionalExcel("European");
                var mStyle = MarginType.OptionalExcel("FuturesStyle");

                if (!Enum.TryParse(CallPut, true, out OptionType cp))
                    throw new Exception($"Could not parse call/put flag {cp}");
                if (!Enum.TryParse(exStyle, true, out OptionExerciseType exType))
                    throw new Exception($"Could not parse option style flag {exStyle}");
                if (!Enum.TryParse(mStyle, true, out OptionMarginingType mType))
                    throw new Exception($"Could not parse margining type flag {mType}");

                var product = new FuturesOption
                {
                    AssetId = AssetId,
                    ContractQuantity = Quantity,
                    LotSize = LotSize,
                    PriceMultiplier = 1.0,
                    Currency = currency,
                    Strike = Strike,
                    Direction = TradeDirection.Long,
                    ExpiryDate = ExpiryDate,
                    TradeId = ObjectName,
                    CallPut = cp,
                    ExerciseType = exType,
                    MarginingType = mType
                };

                var cache = ContainerStores.GetObjectCache<FuturesOption>();
                cache.PutObject(ObjectName, new SessionItem<FuturesOption> { Name = ObjectName, Value = product });
                return ObjectName + '¬' + cache.GetObject(ObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Creates a monthly-settled asian swap", Category = CategoryNames.Instruments, Name = CategoryNames.Instruments + "_" + nameof(CreateMonthlyAsianSwap))]
        public static object CreateMonthlyAsianSwap(
             [ExcelArgument(Description = "Object name")] string ObjectName,
             [ExcelArgument(Description = "Period code or dates")] object PeriodCodeOrDates,
             [ExcelArgument(Description = "Asset Id")] string AssetId,
             [ExcelArgument(Description = "Currency")] string Currency,
             [ExcelArgument(Description = "Strike")] double Strike,
             [ExcelArgument(Description = "Notional")] double Notional,
             [ExcelArgument(Description = "Fixing calendar")] object FixingCalendar,
             [ExcelArgument(Description = "Payment calendar")] object PaymentCalendar,
             [ExcelArgument(Description = "Payment offset or date")] object PaymentOffset,
             [ExcelArgument(Description = "Spot lag")] object SpotLag,
             [ExcelArgument(Description = "Fixing date generation type")] object DateGenerationType,
             [ExcelArgument(Description = "Discount curve")] string DiscountCurve)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var fixingCal = FixingCalendar.OptionalExcel("WeekendsOnly");
                var paymentCal = PaymentCalendar.OptionalExcel("WeekendsOnly");
                var spotLag = SpotLag.OptionalExcel("0b");
                var dGenType = DateGenerationType.OptionalExcel("BusinessDays");
                var paymentOffset = PaymentOffset.OptionalExcel("0b");

                if (!ContainerStores.SessionContainer.GetService<ICalendarProvider>().Collection.TryGetCalendar(fixingCal, out var fCal))
                {
                    _logger?.LogInformation("Calendar {calendar} not found in cache", fixingCal);
                    return $"Calendar {fixingCal} not found in cache";
                }
                if (!ContainerStores.SessionContainer.GetService<ICalendarProvider>().Collection.TryGetCalendar(paymentCal, out var pCal))
                {
                    _logger?.LogInformation("Calendar {calendar} not found in cache", paymentCal);
                    return $"Calendar {paymentCal} not found in cache";
                }
                var pOffset = new Frequency(paymentOffset);
                var sLag = new Frequency(spotLag);
                if (!Enum.TryParse(dGenType, out DateGenerationType dType))
                {
                    return $"Could not parse date generation type - {dGenType}";
                }
                var currency = ContainerStores.GlobalContainer.GetRequiredService<ICurrencyProvider>()[Currency];

                AsianSwapStrip product;
                if (PeriodCodeOrDates is object[,])
                {
                    var dates = ((object[,])PeriodCodeOrDates).ObjectRangeToVector<double>().ToDateTimeArray();
                    product = AssetProductFactory.CreateMonthlyAsianSwap(dates[0], dates[1], Strike, AssetId, fCal, pCal, pOffset, currency, TradeDirection.Long, sLag, Notional, dType);
                }
                else if (PeriodCodeOrDates is double)
                {
                    PeriodCodeOrDates = DateTime.FromOADate((double)PeriodCodeOrDates).ToString("MMM-yy");
                    product = AssetProductFactory.CreateMonthlyAsianSwap(PeriodCodeOrDates as string, Strike, AssetId, fCal, pCal, pOffset, currency, TradeDirection.Long, sLag, Notional, dType);
                }
                else
                    product = AssetProductFactory.CreateMonthlyAsianSwap(PeriodCodeOrDates as string, Strike, AssetId, fCal, pCal, pOffset, currency, TradeDirection.Long, sLag, Notional, dType);

                product.TradeId = ObjectName;
                foreach(var s in product.Swaplets)
                {
                    s.DiscountCurve = DiscountCurve;
                }

                var cache = ContainerStores.GetObjectCache<AsianSwapStrip>();
                cache.PutObject(ObjectName, new SessionItem<AsianSwapStrip> { Name = ObjectName, Value = product });
                return ObjectName + '¬' + cache.GetObject(ObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Creates an asian swap with custom pricing periods", Category = CategoryNames.Instruments, Name = CategoryNames.Instruments + "_" + nameof(CreateCustomAsianSwap))]
        public static object CreateCustomAsianSwap(
             [ExcelArgument(Description = "Object name")] string ObjectName,
             [ExcelArgument(Description = "Period dates")] object PeriodDates,
             [ExcelArgument(Description = "Asset Id")] string AssetId,
             [ExcelArgument(Description = "Currency")] string Currency,
             [ExcelArgument(Description = "Strike")] double Strike,
             [ExcelArgument(Description = "Notionals")] double[] Notionals,
             [ExcelArgument(Description = "Fixing calendar")] object FixingCalendar,
             [ExcelArgument(Description = "Payment calendar")] object PaymentCalendar,
             [ExcelArgument(Description = "Payment offset")] object PaymentOffset,
             [ExcelArgument(Description = "Spot lag")] object SpotLag,
             [ExcelArgument(Description = "Fixing date generation type")] object DateGenerationType,
             [ExcelArgument(Description = "Discount curve")] string DiscountCurve)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var fixingCal = FixingCalendar.OptionalExcel("WeekendsOnly");
                var paymentCal = PaymentCalendar.OptionalExcel("WeekendsOnly");
                var spotLag = SpotLag.OptionalExcel("0b");
                var dGenType = DateGenerationType.OptionalExcel("BusinessDays");
                var paymentOffset = PaymentOffset.OptionalExcel("0b");

                if (!ContainerStores.SessionContainer.GetService<ICalendarProvider>().Collection.TryGetCalendar(fixingCal, out var fCal))
                {
                    _logger?.LogInformation("Calendar {calendar} not found in cache", fixingCal);
                    return $"Calendar {fixingCal} not found in cache";
                }
                if (!ContainerStores.SessionContainer.GetService<ICalendarProvider>().Collection.TryGetCalendar(paymentCal, out var pCal))
                {
                    _logger?.LogInformation("Calendar {calendar} not found in cache", paymentCal);
                    return $"Calendar {paymentCal} not found in cache";
                }
                var pOffset = new Frequency(paymentOffset);
                var sLag = new Frequency(spotLag);
                if (!Enum.TryParse(dGenType, out DateGenerationType dType))
                {
                    return $"Could not parse date generation type - {dGenType}";
                }
                var currency = ContainerStores.GlobalContainer.GetRequiredService<ICurrencyProvider>()[Currency];

                AsianSwapStrip product;
                if (PeriodDates is object[,] pd && pd.GetLength(1) == 2)
                {
                    if(Notionals.Length!=pd.GetLength(0))
                        throw new Exception("Number of notionals must match number of periods");

                    var doubles = pd.ObjectRangeToMatrix<double>();
                    var swaplets = new List<AsianSwap>();

                    for (var i = 0; i < doubles.GetLength(0); i++)
                    {
                        if (doubles[i, 0] == 0 || doubles[i, 1] == 0)
                            break;

                        var start = DateTime.FromOADate(doubles[i, 0]);
                        var end = DateTime.FromOADate(doubles[i, 1]);
                        swaplets.Add(AssetProductFactory.CreateTermAsianSwap(start, end, Strike, AssetId, fCal, pCal, pOffset, currency, TradeDirection.Long, sLag, Notionals[i], dType));
                    }

                    product = new AsianSwapStrip
                    {
                        Swaplets = swaplets.ToArray()
                    };
                }
                else
                    throw new Exception("Expecting a Nx2 array of period dates");

                product.TradeId = ObjectName;
                foreach (var s in product.Swaplets)
                {
                    s.DiscountCurve = DiscountCurve;
                }

                var cache = ContainerStores.GetObjectCache<AsianSwapStrip>();
                cache.PutObject(ObjectName, new SessionItem<AsianSwapStrip> { Name = ObjectName, Value = product });
                return ObjectName + '¬' + cache.GetObject(ObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Creates an asian option", Category = CategoryNames.Instruments, Name = CategoryNames.Instruments + "_" + nameof(CreateAsianOption))]
        public static object CreateAsianOption(
             [ExcelArgument(Description = "Object name")] string ObjectName,
             [ExcelArgument(Description = "Period code")] object PeriodCodeOrDates,
             [ExcelArgument(Description = "Asset Id")] string AssetId,
             [ExcelArgument(Description = "Currency")] string Currency,
             [ExcelArgument(Description = "Strike")] double Strike,
             [ExcelArgument(Description = "Put/Call")] string PutOrCall,
             [ExcelArgument(Description = "Notional")] double Notional,
             [ExcelArgument(Description = "Fixing calendar")] object FixingCalendar,
             [ExcelArgument(Description = "Payment calendar")] object PaymentCalendar,
             [ExcelArgument(Description = "Payment offset")] object PaymentOffsetOrDate,
             [ExcelArgument(Description = "Spot lag")] object SpotLag,
             [ExcelArgument(Description = "Fixing date generation type")] object DateGenerationType,
             [ExcelArgument(Description = "Discount curve")] string DiscountCurve)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var fixingCal = FixingCalendar.OptionalExcel("WeekendsOnly");
                var paymentCal = PaymentCalendar.OptionalExcel("WeekendsOnly");
                var spotLag = SpotLag.OptionalExcel("0b");
                var dGenType = DateGenerationType.OptionalExcel("BusinessDays");
                var paymentOffset = PaymentOffsetOrDate is double ? "0b" : PaymentOffsetOrDate.OptionalExcel("0b");

                if (!Enum.TryParse(PutOrCall, out OptionType oType))
                {
                    return $"Could not parse put/call flag - {PutOrCall}";
                }

                if (!ContainerStores.SessionContainer.GetService<ICalendarProvider>().Collection.TryGetCalendar(fixingCal, out var fCal))
                {
                    _logger?.LogInformation("Calendar {calendar} not found in cache", fixingCal);
                    return $"Calendar {fixingCal} not found in cache";
                }
                if (!ContainerStores.SessionContainer.GetService<ICalendarProvider>().Collection.TryGetCalendar(paymentCal, out var pCal))
                {
                    _logger?.LogInformation("Calendar {calendar} not found in cache", paymentCal);
                    return $"Calendar {paymentCal} not found in cache";
                }
                var pOffset = new Frequency(paymentOffset);
                var sLag = new Frequency(spotLag);
                if (!Enum.TryParse(dGenType, out DateGenerationType dType))
                {
                    return $"Could not parse date generation type - {dGenType}";
                }
                var currency = ContainerStores.GlobalContainer.GetRequiredService<ICurrencyProvider>()[Currency];

                AsianOption product;
                if (PeriodCodeOrDates is object[,])
                {
                    var dates = ((object[,])PeriodCodeOrDates).ObjectRangeToVector<double>().ToDateTimeArray();
                    if (PaymentOffsetOrDate is double)
                        product = AssetProductFactory.CreateAsianOption(dates[0], dates[1], Strike, AssetId, oType, fCal, DateTime.FromOADate((double)PaymentOffsetOrDate), currency, TradeDirection.Long, sLag, Notional, dType);
                    else
                    {
                        product = AssetProductFactory.CreateAsianOption(dates[0], dates[1], Strike, AssetId, oType, fCal, pCal, pOffset, currency, TradeDirection.Long, sLag, Notional, dType);
                    }
                }
                else if (PeriodCodeOrDates is double)
                {
                    PeriodCodeOrDates = DateTime.FromOADate((double)PeriodCodeOrDates).ToString("MMM-yy");
                    product = AssetProductFactory.CreateAsianOption(PeriodCodeOrDates as string, Strike, AssetId, oType, fCal, pCal, pOffset, currency, TradeDirection.Long, sLag, Notional, dType);
                }
                else
                    product = AssetProductFactory.CreateAsianOption(PeriodCodeOrDates as string, Strike, AssetId, oType, fCal, pCal, pOffset, currency, TradeDirection.Long, sLag, Notional, dType);

                product.TradeId = ObjectName;
                product.DiscountCurve = DiscountCurve;
                
                var cache = ContainerStores.GetObjectCache<AsianOption>();
                cache.PutObject(ObjectName, new SessionItem<AsianOption> { Name = ObjectName, Value = product });
                return ObjectName + '¬' + cache.GetObject(ObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Creates an asian lookback option", Category = CategoryNames.Instruments, Name = CategoryNames.Instruments + "_" + nameof(CreateAsianLookbackOption))]
        public static object CreateAsianLookbackOption(
             [ExcelArgument(Description = "Object name")] string ObjectName,
             [ExcelArgument(Description = "Period code")] object PeriodCodeOrDates,
             [ExcelArgument(Description = "Asset Id")] string AssetId,
             [ExcelArgument(Description = "Currency")] string Currency,
             [ExcelArgument(Description = "Put/Call")] string PutOrCall,
             [ExcelArgument(Description = "Notional")] double Notional,
             [ExcelArgument(Description = "Fixing calendar")] object FixingCalendar,
             [ExcelArgument(Description = "Payment calendar")] object PaymentCalendar,
             [ExcelArgument(Description = "Payment offset")] object PaymentOffsetOrDate,
             [ExcelArgument(Description = "Spot lag")] object SpotLag,
             [ExcelArgument(Description = "Fixing date generation type")] object DateGenerationType,
             [ExcelArgument(Description = "Discount curve")] string DiscountCurve)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var fixingCal = FixingCalendar.OptionalExcel("WeekendsOnly");
                var paymentCal = PaymentCalendar.OptionalExcel("WeekendsOnly");
                var spotLag = SpotLag.OptionalExcel("0b");
                var dGenType = DateGenerationType.OptionalExcel("BusinessDays");
                var paymentOffset = PaymentOffsetOrDate is double ? "0b" : PaymentOffsetOrDate.OptionalExcel("0b");

                if (!Enum.TryParse(PutOrCall, out OptionType oType))
                {
                    return $"Could not parse put/call flag - {PutOrCall}";
                }

                if (!ContainerStores.SessionContainer.GetService<ICalendarProvider>().Collection.TryGetCalendar(fixingCal, out var fCal))
                {
                    _logger?.LogInformation("Calendar {calendar} not found in cache", fixingCal);
                    return $"Calendar {fixingCal} not found in cache";
                }
                if (!ContainerStores.SessionContainer.GetService<ICalendarProvider>().Collection.TryGetCalendar(paymentCal, out var pCal))
                {
                    _logger?.LogInformation("Calendar {calendar} not found in cache", paymentCal);
                    return $"Calendar {paymentCal} not found in cache";
                }
                var pOffset = new Frequency(paymentOffset);
                var sLag = new Frequency(spotLag);
                if (!Enum.TryParse(dGenType, out DateGenerationType dType))
                {
                    return $"Could not parse date generation type - {dGenType}";
                }
                var currency = ContainerStores.GlobalContainer.GetRequiredService<ICurrencyProvider>().GetCurrency(Currency);

                AsianLookbackOption product;
                if (PeriodCodeOrDates is object[,])
                {
                    var dates = ((object[,])PeriodCodeOrDates).ObjectRangeToVector<double>().ToDateTimeArray();
                    if (PaymentOffsetOrDate is double)
                        product = AssetProductFactory.CreateAsianLookbackOption(dates[0], dates[1], AssetId, oType, fCal, DateTime.FromOADate((double)PaymentOffsetOrDate), currency, TradeDirection.Long, sLag, Notional, dType);
                    else
                    {
                        product = AssetProductFactory.CreateAsianLookbackOption(dates[0], dates[1], AssetId, oType, fCal, pCal, pOffset, currency, TradeDirection.Long, sLag, Notional, dType);
                    }
                }
                else if (PeriodCodeOrDates is double)
                {
                    PeriodCodeOrDates = DateTime.FromOADate((double)PeriodCodeOrDates).ToString("MMM-yy");
                    product = AssetProductFactory.CreateAsianLookbackOption(PeriodCodeOrDates as string, AssetId, oType, fCal, pCal, pOffset, currency, TradeDirection.Long, sLag, Notional, dType);
                }
                else
                    product = AssetProductFactory.CreateAsianLookbackOption(PeriodCodeOrDates as string, AssetId, oType, fCal, pCal, pOffset, currency, TradeDirection.Long, sLag, Notional, dType);

                product.TradeId = ObjectName;
                product.DiscountCurve = DiscountCurve;

                var cache = ContainerStores.GetObjectCache<AsianLookbackOption>();
                cache.PutObject(ObjectName, new SessionItem<AsianLookbackOption> { Name = ObjectName, Value = product });
                return ObjectName + '¬' + cache.GetObject(ObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Creates a european option with a continuous american barrier", Category = CategoryNames.Instruments, Name = CategoryNames.Instruments + "_" + nameof(CreateAmericanBarrierOption))]
        public static object CreateAmericanBarrierOption(
             [ExcelArgument(Description = "Object name")] string ObjectName,
             [ExcelArgument(Description = "Asset Id")] string AssetId,
             [ExcelArgument(Description = "Barrier observation start date")] DateTime ObservationStart,
             [ExcelArgument(Description = "Barrier observation end date")] DateTime ObservationEnd,
             [ExcelArgument(Description = "Option expiry date")] DateTime Expiry,
             [ExcelArgument(Description = "Payment date")] DateTime PayDate,
             [ExcelArgument(Description = "Currency")] string Currency,
             [ExcelArgument(Description = "Put/Call")] string PutOrCall,
             [ExcelArgument(Description = "Notional")] double Notional,
             [ExcelArgument(Description = "Strike")] double Strike,
             [ExcelArgument(Description = "Barrier level")] double Barrier,
             [ExcelArgument(Description = "Barrier side, Up or Down")] object BarrierSide,
             [ExcelArgument(Description = "Barrier type, KI or KO")] object BarrierType,
             [ExcelArgument(Description = "Discount curve")] string DiscountCurve,
             [ExcelArgument(Description = "Spot lag: 0b (Energy), 2b (PM) or 2w (BM) typically")] object SpotLag,
             [ExcelArgument(Description = "Fixing calendar for spot lag")] object FixingCalendar)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var bSideStr = BarrierSide.OptionalExcel("Up");
                var bTypeStr = BarrierType.OptionalExcel("KI");
                var spotLag = SpotLag.OptionalExcel("0b");
                var fCal = FixingCalendar.OptionalExcel(Currency);

                if (!Enum.TryParse(bSideStr, out BarrierSide bSide))
                {
                    return $"Could not parse barrier side {bSideStr}";
                }
                if (!Enum.TryParse(bTypeStr, out BarrierType bType))
                {
                    return $"Could not parse barrier type {bTypeStr}";
                }
                if (!Enum.TryParse(PutOrCall, out OptionType oType))
                {
                    return $"Could not parse put/call flag - {PutOrCall}";
                }
                var currency = ContainerStores.GlobalContainer.GetRequiredService<ICurrencyProvider>().GetCurrency(Currency);
                var sLag = new Frequency(spotLag);
                if (!ContainerStores.SessionContainer.GetService<ICalendarProvider>().Collection.TryGetCalendar(fCal, out var cal))
                {
                    _logger?.LogInformation("Calendar {calendar} not found in cache", fCal);
                    return $"Calendar {fCal} not found in cache";
                }

                var product = new EuropeanBarrierOption
                {
                    AssetId = AssetId,
                    Barrier = Barrier,
                    BarrierObservationEndDate = ObservationEnd,
                    BarrierObservationStartDate = ObservationStart,
                    BarrierType = bType,
                    BarrierObservationType = BarrierObservationType.Continuous,
                    BarrierSide = bSide,
                    CallPut = oType,
                    Direction = TradeDirection.Long,
                    DiscountCurve = DiscountCurve,
                    ExpiryDate = Expiry,
                    Notional = Notional,
                    PaymentCurrency = currency,
                    PaymentDate = PayDate,
                    Strike = Strike,
                    TradeId = ObjectName,
                    SpotLag = sLag,
                    FixingCalendar = cal
                };

                var cache = ContainerStores.GetObjectCache<EuropeanBarrierOption>();
                cache.PutObject(ObjectName, new SessionItem<EuropeanBarrierOption> { Name = ObjectName, Value = product });
                return ObjectName + '¬' + cache.GetObject(ObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Returns par rate of a trade given an AssetFx model", Category = CategoryNames.Instruments, Name = CategoryNames.Instruments + "_" + nameof(ProductParRate))]
        public static object ProductParRate(
           [ExcelArgument(Description = "Trade object name")] string TradeName,
           [ExcelArgument(Description = "Asset-FX model name")] string ModelName)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                if (!ContainerStores.GetObjectCache<IAssetFxModel>().TryGetObject(ModelName, out var model))
                    throw new Exception($"Could not find model with name {ModelName}");

                var pf = GetPortfolio(new[] { TradeName });

                if(!pf.Instruments.Any())
                    throw new Exception($"Could not find any trade with name {TradeName}");

                if (!(pf.Instruments.First() is IAssetInstrument trade))
                    throw new Exception($"Could not find asset trade with name {TradeName}");

                var result = trade.ParRate(model.Value);

                return result;
            });
        }

        [ExcelFunction(Description = "Returns PV of a trade given an AssetFx model", Category = CategoryNames.Instruments, Name = CategoryNames.Instruments + "_" + nameof(ProductPV))]
        public static object ProductPV(
            [ExcelArgument(Description = "Trade object name")] string TradeName,
            [ExcelArgument(Description = "Asset-FX model name")] string ModelName,
            [ExcelArgument(Description = "Reporting currency (optional)")] object ReportingCcy)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                if (!ContainerStores.GetObjectCache<IAssetFxModel>().TryGetObject(ModelName, out var model))
                    throw new Exception($"Could not find model with name {ModelName}");

                var pf = GetPortfolio(new[] { TradeName });

                if (!pf.Instruments.Any())
                    throw new Exception($"Could not find any trade with name {TradeName}");

                if (!(pf.Instruments.First() is IAssetInstrument trade))
                    throw new Exception($"Could not find asset trade with name {TradeName}");

                Currency ccy = null;
                if (!(ReportingCcy is ExcelMissing))
                    ccy = ContainerStores.CurrencyProvider.GetCurrency(ReportingCcy as string);

                var result = pf.PV(model.Value, ccy);

                return result.GetAllRows().First().Value;
            });
        }

        [ExcelFunction(Description = "Returns PV of a portfolio given an AssetFx model", Category = CategoryNames.Instruments, Name = CategoryNames.Instruments + "_" + nameof(AssetPortfolioPV))]
        public static object AssetPortfolioPV(
           [ExcelArgument(Description = "Result object name")] string ResultObjectName,
           [ExcelArgument(Description = "Portolio object name")] string PortfolioName,
           [ExcelArgument(Description = "Asset-FX model name")] string ModelName,
           [ExcelArgument(Description = "Reporting currency (optional)")] object ReportingCcy)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var pfolio = GetPortfolioOrTradeFromCache(PortfolioName);
                var model = ContainerStores.GetObjectCache<IAssetFxModel>()
                .GetObjectOrThrow(ModelName, $"Could not find model with name {ModelName}");

                Currency ccy = null;
                if (!(ReportingCcy is ExcelMissing))
                {
                    ccy = ContainerStores.CurrencyProvider[ReportingCcy as string];
                }

                var result = pfolio.PV(model.Value, ccy);
                var resultCache = ContainerStores.GetObjectCache<ICube>();
                resultCache.PutObject(ResultObjectName, new SessionItem<ICube> { Name = ResultObjectName, Value = result });
                return ResultObjectName + '¬' + resultCache.GetObject(ResultObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Returns asset delta of a portfolio given an AssetFx model", Category = CategoryNames.Instruments, Name = CategoryNames.Instruments + "_" + nameof(AssetPortfolioDelta))]
        public static object AssetPortfolioDelta(
            [ExcelArgument(Description = "Result object name")] string ResultObjectName,
            [ExcelArgument(Description = "Portolio object name")] string PortfolioName,
            [ExcelArgument(Description = "Asset-FX model name")] string ModelName)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var pfolio = GetPortfolioOrTradeFromCache(PortfolioName);
                var model = ContainerStores.GetObjectCache<IAssetFxModel>()
                .GetObjectOrThrow(ModelName, $"Could not find model with name {ModelName}");

                var result = pfolio.AssetDelta(model.Value);
                var resultCache = ContainerStores.GetObjectCache<ICube>();
                resultCache.PutObject(ResultObjectName, new SessionItem<ICube> { Name = ResultObjectName, Value = result });
                return ResultObjectName + '¬' + resultCache.GetObject(ResultObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Returns asset delta and gamma of a portfolio given an AssetFx model", Category = CategoryNames.Instruments, Name = CategoryNames.Instruments + "_" + nameof(AssetPortfolioDeltaGamma))]
        public static object AssetPortfolioDeltaGamma(
            [ExcelArgument(Description = "Result object name")] string ResultObjectName,
            [ExcelArgument(Description = "Portolio object name")] string PortfolioName,
            [ExcelArgument(Description = "Asset-FX model name")] string ModelName)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var pfolio = GetPortfolioOrTradeFromCache(PortfolioName);
                var model = ContainerStores.GetObjectCache<IAssetFxModel>()
                .GetObjectOrThrow(ModelName, $"Could not find model with name {ModelName}");

                var result = pfolio.AssetDeltaGamma(model.Value);
                var resultCache = ContainerStores.GetObjectCache<ICube>();
                resultCache.PutObject(ResultObjectName, new SessionItem<ICube> { Name = ResultObjectName, Value = result });
                return ResultObjectName + '¬' + resultCache.GetObject(ResultObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Returns asset vega of a portfolio given an AssetFx model", Category = CategoryNames.Instruments, Name = CategoryNames.Instruments + "_" + nameof(AssetPortfolioVega))]
        public static object AssetPortfolioVega(
            [ExcelArgument(Description = "Result object name")] string ResultObjectName,
            [ExcelArgument(Description = "Portolio object name")] string PortfolioName,
            [ExcelArgument(Description = "Asset-FX model name")] string ModelName,
            [ExcelArgument(Description = "Reporting currency")] string ReportingCcy)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var pfolio = GetPortfolioOrTradeFromCache(PortfolioName);
                var model = ContainerStores.GetObjectCache<IAssetFxModel>()
                .GetObjectOrThrow(ModelName, $"Could not find model with name {ModelName}");

                var ccy = ContainerStores.CurrencyProvider[ReportingCcy];
                var result = pfolio.AssetVega(model.Value, ccy);
                var resultCache = ContainerStores.GetObjectCache<ICube>();
                resultCache.PutObject(ResultObjectName, new SessionItem<ICube> { Name = ResultObjectName, Value = result });
                return ResultObjectName + '¬' + resultCache.GetObject(ResultObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Returns fx vega of a portfolio given an AssetFx model", Category = CategoryNames.Instruments, Name = CategoryNames.Instruments + "_" + nameof(AssetPortfolioFxVega))]
        public static object AssetPortfolioFxVega(
            [ExcelArgument(Description = "Result object name")] string ResultObjectName,
            [ExcelArgument(Description = "Portolio object name")] string PortfolioName,
            [ExcelArgument(Description = "Asset-FX model name")] string ModelName,
            [ExcelArgument(Description = "Reporting currency")] string ReportingCcy)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var pfolio = GetPortfolioOrTradeFromCache(PortfolioName);
                var model = ContainerStores.GetObjectCache<IAssetFxModel>()
                .GetObjectOrThrow(ModelName, $"Could not find model with name {ModelName}");

                var ccy = ContainerStores.GlobalContainer.GetRequiredService<ICurrencyProvider>()[ReportingCcy];
                var result = pfolio.FxVega(model.Value, ccy);
                var resultCache = ContainerStores.GetObjectCache<ICube>();
                resultCache.PutObject(ResultObjectName, new SessionItem<ICube> { Name = ResultObjectName, Value = result });
                return ResultObjectName + '¬' + resultCache.GetObject(ResultObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Returns correlation delta of a portfolio given an AssetFx model", Category = CategoryNames.Instruments, Name = CategoryNames.Instruments + "_" + nameof(AssetPortfolioCorrelationDelta))]
        public static object AssetPortfolioCorrelationDelta(
            [ExcelArgument(Description = "Result object name")] string ResultObjectName,
            [ExcelArgument(Description = "Portolio object name")] string PortfolioName,
            [ExcelArgument(Description = "Asset-FX model name")] string ModelName,
            [ExcelArgument(Description = "Reporting currency")] string ReportingCcy,
            [ExcelArgument(Description = "Epsilon bump size, rho' = rho + epsilon * (1-rho)")] double Epsilon)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var pfolio = GetPortfolioOrTradeFromCache(PortfolioName);
                var model = ContainerStores.GetObjectCache<IAssetFxModel>()
                .GetObjectOrThrow(ModelName, $"Could not find model with name {ModelName}");

                var ccy = ContainerStores.CurrencyProvider[ReportingCcy];
                var result = pfolio.CorrelationDelta(model.Value, ccy, Epsilon);
                var resultCache = ContainerStores.GetObjectCache<ICube>();
                resultCache.PutObject(ResultObjectName, new SessionItem<ICube> { Name = ResultObjectName, Value = result });
                return ResultObjectName + '¬' + resultCache.GetObject(ResultObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Returns fx delta of a portfolio given an AssetFx model", Category = CategoryNames.Instruments, Name = CategoryNames.Instruments + "_" + nameof(AssetPortfolioFxDelta))]
        public static object AssetPortfolioFxDelta(
            [ExcelArgument(Description = "Result object name")] string ResultObjectName,
            [ExcelArgument(Description = "Portolio object name")] string PortfolioName,
            [ExcelArgument(Description = "Asset-FX model name")] string ModelName,
            [ExcelArgument(Description = "Home currency, e.g. ZAR")] string HomeCcy)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var pfolio = GetPortfolioOrTradeFromCache(PortfolioName);
                var model = ContainerStores.GetObjectCache<IAssetFxModel>()
                .GetObjectOrThrow(ModelName, $"Could not find model with name {ModelName}");
                var ccy = ContainerStores.CurrencyProvider[HomeCcy];

                var result = pfolio.FxDelta(model.Value, ccy, ContainerStores.CurrencyProvider);
                var resultCache = ContainerStores.GetObjectCache<ICube>();
                resultCache.PutObject(ResultObjectName, new SessionItem<ICube> { Name = ResultObjectName, Value = result });
                return ResultObjectName + '¬' + resultCache.GetObject(ResultObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Returns theta of a portfolio given an AssetFx model", Category = CategoryNames.Instruments, Name = CategoryNames.Instruments + "_" + nameof(AssetPortfolioTheta))]
        public static object AssetPortfolioTheta(
           [ExcelArgument(Description = "Result object name")] string ResultObjectName,
           [ExcelArgument(Description = "Portolio object name")] string PortfolioName,
           [ExcelArgument(Description = "Asset-FX model name")] string ModelName,
           [ExcelArgument(Description = "Fwd value date, usually T+1")] DateTime FwdValDate,
           [ExcelArgument(Description = "Reporting currency")] string ReportingCcy)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var pfolio = GetPortfolioOrTradeFromCache(PortfolioName);
                var model = ContainerStores.GetObjectCache<IAssetFxModel>()
                .GetObjectOrThrow(ModelName, $"Could not find model with name {ModelName}");

                var ccy = ContainerStores.CurrencyProvider[ReportingCcy];
                var result = pfolio.AssetTheta(model.Value, FwdValDate, ccy, ContainerStores.CurrencyProvider);
                var resultCache = ContainerStores.GetObjectCache<ICube>();
                resultCache.PutObject(ResultObjectName, new SessionItem<ICube> { Name = ResultObjectName, Value = result });
                return ResultObjectName + '¬' + resultCache.GetObject(ResultObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Returns theta and charm of a portfolio given an AssetFx model", Category = CategoryNames.Instruments, Name = CategoryNames.Instruments + "_" + nameof(AssetPortfolioThetaCharm))]
        public static object AssetPortfolioThetaCharm(
            [ExcelArgument(Description = "Result object name")] string ResultObjectName,
            [ExcelArgument(Description = "Portolio object name")] string PortfolioName,
            [ExcelArgument(Description = "Asset-FX model name")] string ModelName,
            [ExcelArgument(Description = "Fwd value date, usually T+1")] DateTime FwdValDate,
            [ExcelArgument(Description = "Reporting currency")] string ReportingCcy)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var pfolio = GetPortfolioOrTradeFromCache(PortfolioName);
                var model = ContainerStores.GetObjectCache<IAssetFxModel>()
                .GetObjectOrThrow(ModelName, $"Could not find model with name {ModelName}");

                var ccy = ContainerStores.CurrencyProvider[ReportingCcy];
                var result = pfolio.AssetThetaCharm(model.Value, FwdValDate, ccy, ContainerStores.CurrencyProvider);
                var resultCache = ContainerStores.GetObjectCache<ICube>();
                resultCache.PutObject(ResultObjectName, new SessionItem<ICube> { Name = ResultObjectName, Value = result });
                return ResultObjectName + '¬' + resultCache.GetObject(ResultObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Returns interest rate delta cube of a portfolio given an AssetFx model", Category = CategoryNames.Instruments, Name = CategoryNames.Instruments + "_" + nameof(AssetPortfolioIrDelta))]
        public static object AssetPortfolioIrDelta(
            [ExcelArgument(Description = "Result object name")] string ResultObjectName,
            [ExcelArgument(Description = "Portolio object name")] string PortfolioName,
            [ExcelArgument(Description = "Asset-FX model name")] string ModelName)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var pfolio = GetPortfolioOrTradeFromCache(PortfolioName);
                var model = ContainerStores.GetObjectCache<IAssetFxModel>()
                .GetObjectOrThrow(ModelName, $"Could not find model with name {ModelName}");

                var result = pfolio.AssetIrDelta(model.Value);
                var resultCache = ContainerStores.GetObjectCache<ICube>();
                resultCache.PutObject(ResultObjectName, new SessionItem<ICube> { Name = ResultObjectName, Value = result });
                return ResultObjectName + '¬' + resultCache.GetObject(ResultObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Returns greeks cube of a portfolio given an AssetFx model", Category = CategoryNames.Instruments, Name = CategoryNames.Instruments + "_" + nameof(AssetPortfolioGreeks))]
        public static object AssetPortfolioGreeks(
            [ExcelArgument(Description = "Result object name")] string ResultObjectName,
            [ExcelArgument(Description = "Portolio object name")] string PortfolioName,
            [ExcelArgument(Description = "Asset-FX model name")] string ModelName,
            [ExcelArgument(Description = "Fwd value date, usually T+1")] DateTime FwdValDate,
            [ExcelArgument(Description = "Reporting currency")] string ReportingCcy)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var pfolio = GetPortfolioOrTradeFromCache(PortfolioName);
                var model = ContainerStores.GetObjectCache<IAssetFxModel>()
                .GetObjectOrThrow(ModelName, $"Could not find model with name {ModelName}");

                var ccy = ContainerStores.CurrencyProvider.GetCurrency(ReportingCcy);
                var result = pfolio.AssetGreeks(model.Value, FwdValDate, ccy, ContainerStores.CurrencyProvider).Result;
                var resultCache = ContainerStores.GetObjectCache<ICube>();
                resultCache.PutObject(ResultObjectName, new SessionItem<ICube> { Name = ResultObjectName, Value = result });
                return ResultObjectName + '¬' + resultCache.GetObject(ResultObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Returns risk ladder for a portfolio given an AssetFx model and some bump parameters", Category = CategoryNames.Instruments, Name = CategoryNames.Instruments + "_" + nameof(AssetPortfolioRiskLadder))]
        public static object AssetPortfolioRiskLadder(
            [ExcelArgument(Description = "Result object name")] string ResultObjectName,
            [ExcelArgument(Description = "Portolio object name")] string PortfolioName,
            [ExcelArgument(Description = "Asset-FX model name")] string ModelName,
            [ExcelArgument(Description = "Asset Id to bump")] string AssetId,
            [ExcelArgument(Description = "Bump type, defualt FlatShift")] object BumpType,
            [ExcelArgument(Description = "Number of bumps (returns 2*N+1 values)")] int NScenarios,
            [ExcelArgument(Description = "Bump step size")] double BumpStep,
            [ExcelArgument(Description = "Risk metric to produce for each scenario")] object RiskMetric,
            [ExcelArgument(Description = "Return differential to base case, default True")] object ReturnDiff)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var pfolio = GetPortfolioOrTradeFromCache(PortfolioName);
                var model = ContainerStores.GetObjectCache<IAssetFxModel>()
                .GetObjectOrThrow(ModelName, $"Could not find model with name {ModelName}");

                if (!Enum.TryParse(BumpType.OptionalExcel("FlatShift"), out MutationType bType))
                    throw new Exception($"Unknown bump/mutation type {BumpType}");
                if (!Enum.TryParse(RiskMetric.OptionalExcel("AssetCurveDelta"), out RiskMetric metric))
                    throw new Exception($"Unknown risk metric {RiskMetric}");

                if (!bool.TryParse(ReturnDiff.OptionalExcel("True"), out var retDiff))
                    throw new Exception($"Could not parse differential flag {ReturnDiff}");

                var riskLadder = new RiskLadder(AssetId, bType, metric, BumpStep, NScenarios, retDiff);
                var result = riskLadder.Generate(model.Value, pfolio);
                var resultCache = ContainerStores.GetObjectCache<ICube>();
                resultCache.PutObject(ResultObjectName, new SessionItem<ICube> { Name = ResultObjectName, Value = result });
                return ResultObjectName + '¬' + resultCache.GetObject(ResultObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Returns time ladder for a portfolio given an AssetFx model and some bump parameters", Category = CategoryNames.Instruments, Name = CategoryNames.Instruments + "_" + nameof(AssetPortfolioTimeLadder))]
        public static object AssetPortfolioTimeLadder(
            [ExcelArgument(Description = "Result object name")] string ResultObjectName,
            [ExcelArgument(Description = "Portolio object name")] string PortfolioName,
            [ExcelArgument(Description = "Asset-FX model name")] string ModelName,
            [ExcelArgument(Description = "Number of bumps (returns 2*N+1 values)")] int NScenarios,
            [ExcelArgument(Description = "Calendar, default ZAR")] object Calendar,
            [ExcelArgument(Description = "Risk metric to produce for each scenario")] object RiskMetric,
            [ExcelArgument(Description = "Return differential to base case, default True")] object ReturnDiff)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var pfolio = GetPortfolioOrTradeFromCache(PortfolioName);
                var model = ContainerStores.GetObjectCache<IAssetFxModel>()
                .GetObjectOrThrow(ModelName, $"Could not find model with name {ModelName}");

                var fCal = Calendar.OptionalExcel("ZAR");
                if (!ContainerStores.SessionContainer.GetService<ICalendarProvider>().Collection.TryGetCalendar(fCal, out var cal))
                {
                    _logger?.LogInformation("Calendar {calendar} not found in cache", fCal);
                    return $"Calendar {fCal} not found in cache";
                }

                if (!Enum.TryParse(RiskMetric.OptionalExcel("AssetCurveDelta"), out RiskMetric metric))
                    throw new Exception($"Unknown risk metric {RiskMetric}");

                if (!bool.TryParse(ReturnDiff.OptionalExcel("True"), out var retDiff))
                    throw new Exception($"Could not parse differential flag {ReturnDiff}");

                var riskLadder = new TimeLadder(metric, NScenarios, cal, ContainerStores.CurrencyProvider, retDiff);
                var result = riskLadder.Generate(model.Value, pfolio);
                var resultCache = ContainerStores.GetObjectCache<ICube>();
                resultCache.PutObject(ResultObjectName, new SessionItem<ICube> { Name = ResultObjectName, Value = result });
                return ResultObjectName + '¬' + resultCache.GetObject(ResultObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Returns an asset/currency risk matrix for a portfolio given an AssetFx model and some bump parameters", Category = CategoryNames.Instruments, Name = CategoryNames.Instruments + "_" + nameof(AssetPortfolioRiskMatrix))]
        public static object AssetPortfolioRiskMatrix(
            [ExcelArgument(Description = "Result object name")] string ResultObjectName,
            [ExcelArgument(Description = "Portolio object name")] string PortfolioName,
            [ExcelArgument(Description = "Asset-FX model name")] string ModelName,
            [ExcelArgument(Description = "Asset Id to bump")] string AssetId,
            [ExcelArgument(Description = "Currency to bump")] string Currency,
            [ExcelArgument(Description = "Bump type, defualt FlatShift")] object BumpType,
            [ExcelArgument(Description = "Number of bumps (returns 2*N+1 values)")] int NScenarios,
            [ExcelArgument(Description = "Bump step size asset")] double BumpStepAsset,
            [ExcelArgument(Description = "Bump step size fx")] double BumpStepFx,
            [ExcelArgument(Description = "Risk metric to produce for each scenario")] object RiskMetric,
            [ExcelArgument(Description = "Return differential to base case, default True")] object ReturnDiff)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var pfolio = GetPortfolioOrTradeFromCache(PortfolioName);
                var model = ContainerStores.GetObjectCache<IAssetFxModel>()
                .GetObjectOrThrow(ModelName, $"Could not find model with name {ModelName}");

                if (!Enum.TryParse(BumpType.OptionalExcel("FlatShift"), out MutationType bType))
                    throw new Exception($"Unknown bump/mutation type {BumpType}");
                if (!Enum.TryParse(RiskMetric.OptionalExcel("AssetCurveDelta"), out RiskMetric metric))
                    throw new Exception($"Unknown risk metric {RiskMetric}");

                if (!bool.TryParse(ReturnDiff.OptionalExcel("True"), out var retDiff))
                    throw new Exception($"Could not parse differential flag {ReturnDiff}");

                var ccy = ContainerStores.CurrencyProvider.GetCurrency(Currency);

                var riskMatrix = new RiskMatrix(AssetId, ccy, bType, metric, BumpStepAsset, BumpStepFx, NScenarios, ContainerStores.CurrencyProvider, retDiff);
                var result = riskMatrix.Generate(model.Value, pfolio);
                var resultCache = ContainerStores.GetObjectCache<ICube>();
                resultCache.PutObject(ResultObjectName, new SessionItem<ICube> { Name = ResultObjectName, Value = result });
                return ResultObjectName + '¬' + resultCache.GetObject(ResultObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Performs PnL attribution between two AssetFx models", Category = CategoryNames.Instruments, Name = CategoryNames.Instruments + "_" + nameof(AssetPnLAttribution))]
        public static object AssetPnLAttribution(
            [ExcelArgument(Description = "Result object name")] string ResultObjectName,
            [ExcelArgument(Description = "Portolio object name")] string PortfolioName,
            [ExcelArgument(Description = "Starting Asset-FX model name")] string ModelNameStart,
            [ExcelArgument(Description = "Ending Asset-FX model name")] string ModelNameEnd,
            [ExcelArgument(Description = "Reporting currency")] string ReportingCcy)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var pfolio = GetPortfolioOrTradeFromCache(PortfolioName);
                var modelStart = ContainerStores.GetObjectCache<IAssetFxModel>()
                .GetObjectOrThrow(ModelNameStart, $"Could not find model with name {ModelNameStart}");
                var modelEnd = ContainerStores.GetObjectCache<IAssetFxModel>()
                .GetObjectOrThrow(ModelNameEnd, $"Could not find model with name {ModelNameEnd}");
                var ccy = ContainerStores.CurrencyProvider[ReportingCcy];

                var result = pfolio.BasicAttribution(modelStart.Value, modelEnd.Value, ccy, ContainerStores.CurrencyProvider);
                var resultCache = ContainerStores.GetObjectCache<ICube>();
                resultCache.PutObject(ResultObjectName, new SessionItem<ICube> { Name = ResultObjectName, Value = result });
                return ResultObjectName + '¬' + resultCache.GetObject(ResultObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Performs PnL attribution/explain between two AssetFx models", Category = CategoryNames.Instruments, Name = CategoryNames.Instruments + "_" + nameof(AssetPnLAttributionExplain))]
        public static object AssetPnLAttributionExplain(
            [ExcelArgument(Description = "Result object name")] string ResultObjectName,
            [ExcelArgument(Description = "Portolio object name")] string PortfolioName,
            [ExcelArgument(Description = "Starting Asset-FX model name")] string ModelNameStart,
            [ExcelArgument(Description = "Ending Asset-FX model name")] string ModelNameEnd,
            [ExcelArgument(Description = "Starting greeks cube")] string GreeksStart,
            [ExcelArgument(Description = "Reporting currency")] string ReportingCcy)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var pfolio = GetPortfolioOrTradeFromCache(PortfolioName);
                var modelStart = ContainerStores.GetObjectCache<IAssetFxModel>()
                .GetObjectOrThrow(ModelNameStart, $"Could not find model with name {ModelNameStart}");
                var modelEnd = ContainerStores.GetObjectCache<IAssetFxModel>()
                .GetObjectOrThrow(ModelNameEnd, $"Could not find model with name {ModelNameEnd}");
                var greeksStart = ContainerStores.GetObjectCache<ICube>()
                .GetObjectOrThrow(GreeksStart, $"Could not find greeks cube with name {GreeksStart}");
                var ccy = ContainerStores.CurrencyProvider[ReportingCcy];

                var result = pfolio.ExplainAttribution(modelStart.Value, modelEnd.Value, ccy, greeksStart.Value, ContainerStores.CurrencyProvider);
                var resultCache = ContainerStores.GetObjectCache<ICube>();
                resultCache.PutObject(ResultObjectName, new SessionItem<ICube> { Name = ResultObjectName, Value = result });
                return ResultObjectName + '¬' + resultCache.GetObject(ResultObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Performs PnL attribution/explain between two AssetFx models, computing activity PnL", Category = CategoryNames.Instruments, Name = CategoryNames.Instruments + "_" + nameof(AssetPnLAttributionExplainWithActivity))]
        public static object AssetPnLAttributionExplainWithActivity(
            [ExcelArgument(Description = "Result object name")] string ResultObjectName,
            [ExcelArgument(Description = "Starting portolio object name")] string PortfolioStartName,
            [ExcelArgument(Description = "Ending portolio object name")] string PortfolioEndName,
            [ExcelArgument(Description = "Starting Asset-FX model name")] string ModelNameStart,
            [ExcelArgument(Description = "Ending Asset-FX model name")] string ModelNameEnd,
            [ExcelArgument(Description = "Starting greeks cube")] string GreeksStart,
            [ExcelArgument(Description = "Reporting currency")] string ReportingCcy)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var pfolioStart = GetPortfolioOrTradeFromCache(PortfolioStartName);
                var pfolioEnd = GetPortfolioOrTradeFromCache(PortfolioEndName);
                var modelStart = ContainerStores.GetObjectCache<IAssetFxModel>()
                .GetObjectOrThrow(ModelNameStart, $"Could not find model with name {ModelNameStart}");
                var modelEnd = ContainerStores.GetObjectCache<IAssetFxModel>()
                .GetObjectOrThrow(ModelNameEnd, $"Could not find model with name {ModelNameEnd}");
                var greeksStart = ContainerStores.GetObjectCache<ICube>()
                .GetObjectOrThrow(GreeksStart, $"Could not find greeks cube with name {GreeksStart}");
                var ccy = ContainerStores.CurrencyProvider[ReportingCcy];

                var result = pfolioStart.ExplainAttribution(pfolioEnd, modelStart.Value, modelEnd.Value, ccy, greeksStart.Value, ContainerStores.CurrencyProvider);
                var resultCache = ContainerStores.GetObjectCache<ICube>();
                resultCache.PutObject(ResultObjectName, new SessionItem<ICube> { Name = ResultObjectName, Value = result });
                return ResultObjectName + '¬' + resultCache.GetObject(ResultObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Creates a portfolio of instruments", Category = CategoryNames.Instruments, Name = CategoryNames.Instruments + "_" + nameof(CreatePortfolio))]
        public static object CreatePortfolio(
            [ExcelArgument(Description = "Object name")] string ObjectName,
            [ExcelArgument(Description = "Instruments")] object[] Instruments)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var pf = GetPortfolio(Instruments);
                var pFolioCache = ContainerStores.GetObjectCache<Portfolio>();

                pFolioCache.PutObject(ObjectName, new SessionItem<Portfolio> { Name = ObjectName, Value = pf });
                return ObjectName + '¬' + pFolioCache.GetObject(ObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Returns a subset of trades from a portfolio object", Category = CategoryNames.Instruments, Name = CategoryNames.Instruments + "_" + nameof(FilterPortfolio))]
        public static object FilterPortfolio(
            [ExcelArgument(Description = "Output object name")] string ObjectName,
            [ExcelArgument(Description = "Input portfolio object name")] string PortfolioName,
            [ExcelArgument(Description = "Trade Ids")] object[] TradeIds)
        {
            return ExcelHelper.Execute(_logger, () =>
            {

                var pFolioCache = ContainerStores.GetObjectCache<Portfolio>();
                var pfIn = pFolioCache.GetObjectOrThrow(PortfolioName, $"Portfolio {PortfolioName} not found");
                var ids = TradeIds.ObjectRangeToVector<string>();
                var pf = new Portfolio
                {
                    Instruments = new List<IInstrument>
                    (
                        pfIn.Value.Instruments.Where(x => ids.Contains(x.TradeId))
                    )
                };
                pFolioCache.PutObject(ObjectName, new SessionItem<Portfolio> { Name = ObjectName, Value = pf });
                return ObjectName + '¬' + pFolioCache.GetObject(ObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Displays a portfolio of instruments", Category = CategoryNames.Instruments, Name = CategoryNames.Instruments + "_" + nameof(DisplayPortfolio))]
        public static object DisplayPortfolio(
            [ExcelArgument(Description = "Object name")] string ObjectName)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var pf = ContainerStores.GetObjectCache<Portfolio>().GetObjectOrThrow(ObjectName, $"Portfolio {ObjectName} not found");

                return pf.Value.Details();
                
            });
        }

        public static Portfolio GetPortfolioOrTradeFromCache(string name)
        {
            var pfolioCache = ContainerStores.GetObjectCache<Portfolio>();
            if(!pfolioCache.TryGetObject(name, out var pfolio))
            {
                var newPf = GetPortfolio(new[] { name });
                if (newPf.Instruments.Any())
                    return newPf;

                throw new Exception($"Could not find porfolio or trade with name {name}");
            }

            return pfolio.Value;
        }

        public static IPvModel GetModelFromCache(string name, Portfolio portfolio)
        {
            if (ContainerStores.GetObjectCache<IAssetFxModel>().TryGetObject(name, out var modelObj))
            {
                var m = modelObj.Value.Clone();
                m.AttachPortfolio(portfolio);
                return m;
            }
            if (ContainerStores.GetObjectCache<AssetFXMCModelPercursor>().TryGetObject(name, out var precModelObj))
            {
                return precModelObj.Value.ToModel(portfolio);
            }
            throw new Exception($"Could not find model with name {name} in cahce");
        }

        public static Portfolio GetPortfolio(object[] Instruments)
        {
            var swaps = Instruments.GetAnyFromCache<IrSwap>();
            var fras = Instruments.GetAnyFromCache<ForwardRateAgreement>();
            var futures = Instruments.GetAnyFromCache<STIRFuture>();
            var fxFwds = Instruments.GetAnyFromCache<FxForward>();
            var xccySwaps = Instruments.GetAnyFromCache<XccyBasisSwap>();
            var basisSwaps = Instruments.GetAnyFromCache<IrBasisSwap>();
            var loanDepos = Instruments.GetAnyFromCache<FixedRateLoanDeposit>();

            var asianOptions = Instruments.GetAnyFromCache<AsianOption>();
            var asianStrips = Instruments.GetAnyFromCache<AsianSwapStrip>();
            var asianSwaps = Instruments.GetAnyFromCache<AsianSwap>();
            var forwards = Instruments.GetAnyFromCache<Forward>();
            var assetFutures = Instruments.GetAnyFromCache<Future>();
            var europeanOptions = Instruments.GetAnyFromCache<EuropeanOption>();
            var futuresOptions = Instruments.GetAnyFromCache<FuturesOption>();
            var lookbacks = Instruments.GetAnyFromCache<AsianLookbackOption>();

            //allows merging of FICs into portfolios
            var ficInstruments = Instruments.GetAnyFromCache<FundingInstrumentCollection>()
                .SelectMany(s => s);

            //allows merging of portfolios into portfolios
            var pfInstruments = Instruments.GetAnyFromCache<Portfolio>()
                .SelectMany(s => s.Instruments);

            var pf = new Portfolio
            {
                Instruments = new List<IInstrument>()
            };

            pf.Instruments.AddRange(swaps);
            pf.Instruments.AddRange(fras);
            pf.Instruments.AddRange(futures);
            pf.Instruments.AddRange(fxFwds);
            pf.Instruments.AddRange(xccySwaps);
            pf.Instruments.AddRange(basisSwaps);
            pf.Instruments.AddRange(loanDepos);
            pf.Instruments.AddRange(ficInstruments);

            pf.Instruments.AddRange(pfInstruments);
            pf.Instruments.AddRange(asianOptions);
            pf.Instruments.AddRange(asianStrips);
            pf.Instruments.AddRange(asianSwaps);
            pf.Instruments.AddRange(forwards);
            pf.Instruments.AddRange(assetFutures);
            pf.Instruments.AddRange(europeanOptions);
            pf.Instruments.AddRange(futuresOptions);
            pf.Instruments.AddRange(lookbacks);

            return pf;
        }
    }
}
