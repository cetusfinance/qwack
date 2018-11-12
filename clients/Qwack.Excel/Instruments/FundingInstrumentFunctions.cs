using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExcelDna.Integration;
using Qwack.Core.Basic;
using Qwack.Excel.Services;
using Qwack.Excel.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Qwack.Dates;
using Qwack.Core.Instruments.Funding;
using Qwack.Futures;
using Qwack.Core.Models;
using Qwack.Models;

namespace Qwack.Excel.Instruments
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
             [ExcelArgument(Description = "Pay / Receive")] object PayRec,
             [ExcelArgument(Description = "Solve Curve name ")] object SolveCurve,
             [ExcelArgument(Description = "Solve Pillar Date")] object SolvePillarDate)
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

                var solveCurve = SolveCurve.OptionalExcel(rIndex.Name);
                var solvePillarDate = SolvePillarDate.OptionalExcel(product.FlowScheduleFra.Flows.Last().AccrualPeriodEnd);

                product.SolveCurve = solveCurve;
                product.PillarDate = solvePillarDate;
                product.TradeId = ObjectName;

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
           [ExcelArgument(Description = "Solve Curve name ")] string SolveCurve,
           [ExcelArgument(Description = "Solve Pillar Date")] object SolvePillarDate)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var solvePillarDate = SolvePillarDate.OptionalExcel(SettleDate);

                ContainerStores.SessionContainer.GetService<ICalendarProvider>().Collection.TryGetCalendar(DomesticCcy, out var domesticCal);
                ContainerStores.SessionContainer.GetService<ICalendarProvider>().Collection.TryGetCalendar(ForeignCcy, out var foreignCal);


                var product = new FxForward
                {
                    DomesticCCY = ContainerStores.GlobalContainer.GetRequiredService<ICurrencyProvider>()[DomesticCcy],
                    ForeignCCY = ContainerStores.GlobalContainer.GetRequiredService<ICurrencyProvider>()[ForeignCcy],
                    DomesticQuantity = DomesticNotional,
                    DeliveryDate = SettleDate,
                    ForeignDiscountCurve = DiscountCurve,
                    SolveCurve = SolveCurve,
                    PillarDate = solvePillarDate,
                    Strike = Strike,
                    TradeId = ObjectName
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
               [ExcelArgument(Description = "Pay / Receive")] object PayRec,
               [ExcelArgument(Description = "Solve Curve name ")] object SolveCurve,
               [ExcelArgument(Description = "Solve Pillar Date")] object SolvePillarDate)
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

                var solveCurve = SolveCurve.OptionalExcel(rIndex.Name);
                var solvePillarDate = SolvePillarDate.OptionalExcel(product.EndDate);

                product.SolveCurve = solveCurve;
                product.PillarDate = solvePillarDate;
                product.TradeId = ObjectName;

                var cache = ContainerStores.GetObjectCache<IrSwap>();
                cache.PutObject(ObjectName, new SessionItem<IrSwap> { Name = ObjectName, Value = product });
                return ObjectName + '¬' + cache.GetObject(ObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Creates a short-term interest rate future object from a futures code", Category = CategoryNames.Instruments, Name = CategoryNames.Instruments + "_" + nameof(CreateSTIRFromCode))]
        public static object CreateSTIRFromCode(
              [ExcelArgument(Description = "Object name")] string ObjectName,
              [ExcelArgument(Description = "Value date")] DateTime ValDate,
              [ExcelArgument(Description = "Futures Code, e.g. EDZ9")] string FuturesCode,
              [ExcelArgument(Description = "Rate Index")] string RateIndex,
              [ExcelArgument(Description = "Price")] double Price,
              [ExcelArgument(Description = "Quantity in lots")] double Quantity,
              [ExcelArgument(Description = "Convexity adjustment")] double ConvexityAdjustment,
              [ExcelArgument(Description = "Forecast Curve")] string ForecastCurve,
              [ExcelArgument(Description = "Solve Curve name ")] object SolveCurve,
              [ExcelArgument(Description = "Solve Pillar Date")] object SolvePillarDate)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                if (!ContainerStores.GetObjectCache<FloatRateIndex>().TryGetObject(RateIndex, out var rIndex))
                {
                    _logger?.LogInformation("Rate index {index} not found in cache", RateIndex);
                    return $"Rate index {RateIndex} not found in cache";
                }

                var c = new FutureCode(FuturesCode, DateTime.Today.Year - 2, ContainerStores.SessionContainer.GetService<IFutureSettingsProvider>());

                var expiry = c.GetExpiry();
                var accrualStart = expiry.AddPeriod(RollType.F, rIndex.Value.HolidayCalendars, rIndex.Value.FixingOffset);
                var accrualEnd = accrualStart.AddPeriod(rIndex.Value.RollConvention, rIndex.Value.HolidayCalendars, rIndex.Value.ResetTenor);
                var dcf = accrualStart.CalculateYearFraction(accrualEnd, rIndex.Value.DayCountBasis);
                var product = new STIRFuture
                {
                    CCY = rIndex.Value.Currency,
                    ContractSize = c.Settings.LotSize,
                    DCF = dcf,
                    ConvexityAdjustment= ConvexityAdjustment,
                    Expiry =expiry,
                    ForecastCurve = ForecastCurve,
                    Index =rIndex.Value,
                    Position = Quantity,
                    Price= Price,
                };

                var solveCurve = SolveCurve.OptionalExcel(ForecastCurve);
                var solvePillarDate = SolvePillarDate.OptionalExcel(accrualEnd);

                product.SolveCurve = solveCurve;
                product.PillarDate = solvePillarDate;
                product.TradeId = ObjectName;

                var cache = ContainerStores.GetObjectCache<STIRFuture>();
                cache.PutObject(ObjectName, new SessionItem<STIRFuture> { Name = ObjectName, Value = product });
                return ObjectName + '¬' + cache.GetObject(ObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Creates a compounded overnight interest rate future object from a futures code", Category = CategoryNames.Instruments, Name = CategoryNames.Instruments + "_" + nameof(CreateOISFutureFromCode))]
        public static object CreateOISFutureFromCode(
              [ExcelArgument(Description = "Object name")] string ObjectName,
              [ExcelArgument(Description = "Value date")] DateTime ValDate,
              [ExcelArgument(Description = "Futures Code, e.g. EDZ9")] string FuturesCode,
              [ExcelArgument(Description = "Rate Index")] string RateIndex,
              [ExcelArgument(Description = "Price")] double Price,
              [ExcelArgument(Description = "Quantity in lots")] double Quantity,
              [ExcelArgument(Description = "Forecast Curve")] string ForecastCurve,
              [ExcelArgument(Description = "Solve Curve name ")] object SolveCurve,
              [ExcelArgument(Description = "Solve Pillar Date")] object SolvePillarDate)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                if (!ContainerStores.GetObjectCache<FloatRateIndex>().TryGetObject(RateIndex, out var rIndex))
                {
                    _logger?.LogInformation("Rate index {index} not found in cache", RateIndex);
                    return $"Rate index {RateIndex} not found in cache";
                }

                var c = new FutureCode(FuturesCode, DateTime.Today.Year - 2, ContainerStores.SessionContainer.GetService<IFutureSettingsProvider>());

                var expiry = c.GetExpiry();
                var accrualStart = expiry.FirstDayOfMonth();
                var accrualEnd = expiry.LastDayOfMonth();
                var dcf = accrualStart.CalculateYearFraction(accrualEnd, rIndex.Value.DayCountBasis);
                var product = new OISFuture
                {
                    CCY = rIndex.Value.Currency,
                    ContractSize = c.Settings.LotSize,
                    DCF = dcf,
                    AverageStartDate = accrualStart,
                    AverageEndDate = accrualEnd,
                    ForecastCurve = ForecastCurve,
                    Index = rIndex.Value,
                    Position = Quantity,
                    Price = Price,
                };

                var solveCurve = SolveCurve.OptionalExcel(rIndex.Name);
                var solvePillarDate = SolvePillarDate.OptionalExcel(accrualEnd);

                product.SolveCurve = solveCurve;
                product.PillarDate = solvePillarDate;
                product.TradeId = ObjectName;

                var cache = ContainerStores.GetObjectCache<OISFuture>();
                cache.PutObject(ObjectName, new SessionItem<OISFuture> { Name = ObjectName, Value = product });
                return ObjectName + '¬' + cache.GetObject(ObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Creates an interest rate basis swap object following conventions for the given rate index", Category = CategoryNames.Instruments, Name = CategoryNames.Instruments + "_" + nameof(CreateIRBasisSwap))]
        public static object CreateIRBasisSwap(
              [ExcelArgument(Description = "Object name")] string ObjectName,
              [ExcelArgument(Description = "Value date")] DateTime ValDate,
              [ExcelArgument(Description = "Tenor")] string SwapTenor,
              [ExcelArgument(Description = "Rate Index Pay")] string RateIndexPay,
              [ExcelArgument(Description = "Rate Index Receive")] string RateIndexRec,
              [ExcelArgument(Description = "Par Spread Pay")] double ParSpread,
              [ExcelArgument(Description = "Spread on Pay leg?")] object ParSpreadOnPay,
              [ExcelArgument(Description = "Notional")] double Notional,
              [ExcelArgument(Description = "Forecast Curve Pay")] string ForecastCurvePay,
              [ExcelArgument(Description = "Forecast Curve Receive")] string ForecastCurveRec,
              [ExcelArgument(Description = "Discount Curve")] string DiscountCurve,
              [ExcelArgument(Description = "Solve Curve name ")] object SolveCurve,
              [ExcelArgument(Description = "Solve Pillar Date")] object SolvePillarDate)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                if (!ContainerStores.GetObjectCache<FloatRateIndex>().TryGetObject(RateIndexPay, out var rIndexPay))
                {
                    _logger?.LogInformation("Rate index {index} not found in cache", RateIndexPay);
                    return $"Rate index {RateIndexPay} not found in cache";
                }

                if (!ContainerStores.GetObjectCache<FloatRateIndex>().TryGetObject(RateIndexRec, out var rIndexRec))
                {
                    _logger?.LogInformation("Rate index {index} not found in cache", RateIndexRec);
                    return $"Rate index {RateIndexRec} not found in cache";
                }

                var spreadOnPay = ParSpreadOnPay.OptionalExcel(true);

                var tenor = new Frequency(SwapTenor);

                var product = new IrBasisSwap(ValDate, tenor, ParSpread, spreadOnPay, rIndexPay.Value, rIndexRec.Value, ForecastCurvePay, ForecastCurveRec, DiscountCurve);

                var solveCurve = SolveCurve.OptionalExcel(rIndexPay.Name);
                var solvePillarDate = SolvePillarDate.OptionalExcel(product.EndDate);

                product.SolveCurve = solveCurve;
                product.PillarDate = solvePillarDate;
                product.TradeId = ObjectName;

                var cache = ContainerStores.GetObjectCache<IrBasisSwap>();
                cache.PutObject(ObjectName, new SessionItem<IrBasisSwap> { Name = ObjectName, Value = product });
                return ObjectName + '¬' + cache.GetObject(ObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Creates a fixed-rate loan/depo object", Category = CategoryNames.Instruments, Name = CategoryNames.Instruments + "_" + nameof(CreateFixedRateLoanDepo))]
        public static object CreateFixedRateLoanDepo(
             [ExcelArgument(Description = "Object name")] string ObjectName,
             [ExcelArgument(Description = "Start date")] DateTime StartDate,
             [ExcelArgument(Description = "End date")] DateTime EndDate,
             [ExcelArgument(Description = "Fixed rate")] double FixedRate,
             [ExcelArgument(Description = "Daycount basis, e.g. Act360")] string Basis,
             [ExcelArgument(Description = "Currency")] string Currency,
             [ExcelArgument(Description = "Notional, negative for loan")] double Notional,
             [ExcelArgument(Description = "Discount Curve")] string DiscountCurve)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                ContainerStores.SessionContainer.GetService<ICalendarProvider>().Collection.TryGetCalendar(Currency, out var cal);
                var ccy = ContainerStores.GlobalContainer.GetRequiredService<ICurrencyProvider>()[Currency];

                if (!Enum.TryParse(Basis, out DayCountBasis basis))
                {
                    return $"Could not parse daycount basis - {Basis}";
                }

                var product = new FixedRateLoanDeposit(StartDate, EndDate, FixedRate, ccy, basis, Notional, DiscountCurve)
                {
                    TradeId = ObjectName
                };

                var cache = ContainerStores.GetObjectCache<FixedRateLoanDeposit>();
                cache.PutObject(ObjectName, new SessionItem<FixedRateLoanDeposit> { Name = ObjectName, Value = product });
                return ObjectName + '¬' + cache.GetObject(ObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Creates a collection of funding instruments to calibrate a curve engine", Category = CategoryNames.Instruments, Name = CategoryNames.Instruments + "_" + nameof(CreateFundingInstrumentCollection))]
        public static object CreateFundingInstrumentCollection(
           [ExcelArgument(Description = "Object name")] string ObjectName,
           [ExcelArgument(Description = "Instruments")] object[] Instruments)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var swapCache = ContainerStores.GetObjectCache<IrSwap>();
                var swaps = Instruments.GetAnyFromCache<IrSwap>();
                var fras = Instruments.GetAnyFromCache<ForwardRateAgreement>();
                var futures = Instruments.GetAnyFromCache<STIRFuture>();
                var oisFutures = Instruments.GetAnyFromCache<OISFuture>();
                var fxFwds = Instruments.GetAnyFromCache<FxForward>();
                var xccySwaps = Instruments.GetAnyFromCache<XccyBasisSwap>();
                var basisSwaps = Instruments.GetAnyFromCache<IrBasisSwap>();
                var loanDepos = Instruments.GetAnyFromCache<FixedRateLoanDeposit>();

                //allows merging of FICs into portfolios
                var ficInstruments = Instruments.GetAnyFromCache<FundingInstrumentCollection>()
                    .SelectMany(s => s);

                var fic = new FundingInstrumentCollection(ContainerStores.CurrencyProvider);
                fic.AddRange(swaps);
                fic.AddRange(fras);
                fic.AddRange(futures);
                fic.AddRange(oisFutures);
                fic.AddRange(fxFwds);
                fic.AddRange(xccySwaps);
                fic.AddRange(basisSwaps);
                fic.AddRange(ficInstruments);
                fic.AddRange(loanDepos);

                var ficCache = ContainerStores.GetObjectCache<FundingInstrumentCollection>();

                ficCache.PutObject(ObjectName, new SessionItem<FundingInstrumentCollection> { Name = ObjectName, Value = fic });
                return ObjectName + '¬' + ficCache.GetObject(ObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Implies solve stages from a Funding Instrument Collection", Category = CategoryNames.Instruments, Name = CategoryNames.Instruments + "_" + nameof(ImplySolveStages))]
        public static object ImplySolveStages(
            [ExcelArgument(Description = "Funding Instrument Collection Name")] string FICName,
            [ExcelArgument(Description = "Fx Matrix Name")] string FxMatrixName)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var fic = ContainerStores.GetObjectCache<FundingInstrumentCollection>().GetObjectOrThrow(FICName, $"Could not find FIC {FICName}");
                var fx = ContainerStores.GetObjectCache<FxMatrix>().GetObjectOrThrow(FxMatrixName, $"Could not find FxMatrix {FxMatrixName}");

                var stages = fic.Value.ImplySolveStages(fx.Value);

                return stages.DictionaryToRange();
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
                    Currency = ContainerStores.GlobalContainer.GetRequiredService<ICurrencyProvider>()[Currency],
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

        [ExcelFunction(Description = "Creates a new fx spot rate matrix", Category = CategoryNames.Instruments, Name = CategoryNames.Instruments + "_" + nameof(CreateFxMatrix))]
        public static object CreateFxMatrix(
              [ExcelArgument(Description = "Fx matrix name")] string ObjectName,
              [ExcelArgument(Description = "Base currency")] string BaseCurrency,
              [ExcelArgument(Description = "Build date")] DateTime BuildDate,
              [ExcelArgument(Description = "Spot rates")] object[,] SpotRateMap,
              [ExcelArgument(Description = "Fx pair definitions")] object[] FxPairDefinitions,
              [ExcelArgument(Description = "DiscountCurves")] object[,] DiscountCurves)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var fxPairsCache = ContainerStores.GetObjectCache<FxPair>();
                var fxPairs = FxPairDefinitions
                    .Where(s => fxPairsCache.Exists(s as string))
                    .Select(s => fxPairsCache.GetObject(s as string).Value)
                    .ToList();
                var currencies = ContainerStores.GlobalContainer.GetRequiredService<ICurrencyProvider>();

                var spotRatesRaw = SpotRateMap.RangeToDictionary<string, double>();

                var spotRates = spotRatesRaw.ToDictionary(y => currencies[y.Key], y => y.Value);

                var discountCurvesRaw = DiscountCurves.RangeToDictionary<string, string>();
                var discountCurves = discountCurvesRaw.ToDictionary(y => currencies[y.Key], y => y.Value);


                var matrix = new FxMatrix(currencies);
                matrix.Init(currencies[BaseCurrency], BuildDate, spotRates, fxPairs, discountCurves);

                var cache = ContainerStores.GetObjectCache<FxMatrix>();
                cache.PutObject(ObjectName, new SessionItem<FxMatrix> { Name = ObjectName, Value = matrix });
                return ObjectName + '¬' + cache.GetObject(ObjectName).Version;
            });
        }

        [ExcelFunction(Description = "Creates a new fx pair definition", Category = CategoryNames.Instruments, Name = CategoryNames.Instruments + "_" + nameof(CreateFxPair))]
        public static object CreateFxPair(
              [ExcelArgument(Description = "Fx pair name")] string ObjectName,
              [ExcelArgument(Description = "Domestic currency")] string DomesticCurrency,
              [ExcelArgument(Description = "Foreign currency")] string ForeignCurrency,
              [ExcelArgument(Description = "Settlement calendar")] string Calendar,
              [ExcelArgument(Description = "Spot lag")] string SpotLag)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                if (!ContainerStores.SessionContainer.GetService<ICalendarProvider>().Collection.TryGetCalendar(Calendar, out var cal))
                {
                    return $"Calendar {Calendar} not found in cache";
                }

                ContainerStores.SessionContainer.GetService<ICalendarProvider>().Collection.TryGetCalendar(DomesticCurrency, out var domesticCal);
                ContainerStores.SessionContainer.GetService<ICalendarProvider>().Collection.TryGetCalendar(ForeignCurrency, out var foreignCal);

                var pair = new FxPair()
                {
                    Domestic = ContainerStores.GlobalContainer.GetRequiredService<ICurrencyProvider>()[DomesticCurrency],
                    Foreign = ContainerStores.GlobalContainer.GetRequiredService<ICurrencyProvider>()[ForeignCurrency],
                    SettlementCalendar = cal,
                    SpotLag = new Frequency(SpotLag)
                };

                var cache = ContainerStores.GetObjectCache<FxPair>();
                cache.PutObject(ObjectName, new SessionItem<FxPair> { Name = ObjectName, Value = pair });
                return ObjectName + '¬' + cache.GetObject(ObjectName).Version;
            });
        }
    }
}
