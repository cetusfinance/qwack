using System;
using System.Collections.Generic;
using System.Linq;
using Qwack.Core.Basic;
using Qwack.Dates;
using Qwack.Math.Solvers;
using Qwack.Transport.BasicTypes;
using Qwack.Transport.TransportObjects.Instruments;
using Qwack.Transport.TransportObjects.Instruments.Asset;

namespace Qwack.Core.Instruments.Asset
{
    public class Bond : CashAsset
    {
        public double? Coupon { get;set; }
        public CouponFrequency CouponFrequency { get; set; } = CouponFrequency.SemiAnnual;
        public DayCountBasis DayCountbasis { get; set; } = DayCountBasis.Thirty360;
        public StubType StubType { get; set; } = StubType.ShortBoth;

        public DateTime? IssueDate { get; set; }
        public DateTime? MaturityDate { get; set; }
        public DateTime? FirstCouponDate { get; set; }

        public Dictionary<DateTime, double> CallSchedule { get; set; } = [];
        public Dictionary<DateTime, double> SinkingSchedule { get; set; } = [];


        public Bond() : base() { }
        public Bond(double notional, string assetId, Currency ccy, double scalingFactor, Frequency settleLag, Calendar settleCalendar)
            : base(notional, assetId, ccy, scalingFactor, settleLag, settleCalendar)
        {
        }

        public Bond(TO_Bond to, ICurrencyProvider currencyProvider, ICalendarProvider calendarProvider)
            : base(to.Notional, to.AssetId, currencyProvider.GetCurrencySafe(to.Currency), to.ScalingFactor, new Frequency(to.SettleLag??"0b"), calendarProvider.GetCalendarSafe(to.SettleCalendar??to.Currency))
        {
            TradeId = to.TradeId;
            SettleDate = to.SettleDate;
            Price = to.Price;
        }

        public TO_Instrument ToTransportObject() => new()
        {
            AssetInstrumentType = AssetInstrumentType.Bond,
            Bond = new TO_Bond()
            {
                AssetId = AssetId,
                Notional = Notional,
                Counterparty = Counterparty,
                Currency = Currency?.Ccy,
                Price = Price,
                ScalingFactor = ScalingFactor,
                SettleCalendar = SettleCalendar?.Name,
                SettleDate = SettleDate,
                PortfolioName = PortfolioName,
                TradeId = TradeId,
                SettleLag = SettleLag.ToString(),
                MetaData = new(MetaData),
                Coupon = Coupon,
                CallSchedule = CallSchedule,
                SinkingSchedule = SinkingSchedule,
                CouponFrequency = CouponFrequency,
                FirstCouponDate = FirstCouponDate,
                IssueDate = IssueDate,
                MaturityDate = MaturityDate
            }
        };
    }

    public static class BondEx
    {
        public static DateTime WeightedAvgLife(this Bond bond, DateTime valDate)
        {
            if (bond.SinkingSchedule == null && bond.MaturityDate.HasValue)
                return bond.MaturityDate.Value;
            else if (bond.SinkingSchedule == null)
                return valDate;

            var weighted = bond.SinkingSchedule.Sum(x => (x.Key - valDate).TotalDays * x.Value);
            var unWeighted = bond.SinkingSchedule.Sum(x => x.Value);

            var walDays = weighted / unWeighted;

            return valDate.AddDays(walDays).Date;
        }

        public static double YieldToMarketInBase(DateTime spotDate, Dictionary<DateTime, double> schedule, double dirtyPrice, double periodsPerYear, Func<DateTime, double> fxRates, DayCountBasis basis = DayCountBasis.Thirty360)
        {
            var flowsInBase = schedule.ToDictionary(f => DateExtensions.CalculateYearFraction(spotDate, f.Key, basis), f => f.Value * fxRates(f.Key));
            var tPerP = 1 / periodsPerYear;
            return Brent.BrentsMethodSolve(delegate (double ytm)
            {
                var num = 0.0;
                foreach (var item in flowsInBase)
                {
                    var y = (item.Key) / tPerP;
                    num += item.Value / System.Math.Pow(1.0 + ytm / periodsPerYear, y);
                }

                return num - dirtyPrice * fxRates(spotDate);
            }, -0.1, 1.0, 1E-06);
        }

        public static double YieldToMarket(this Bond bond, DateTime spotDate, Func<DateTime, double> fxRates = null)
        {
            fxRates ??= x => 1.0;
            var schedule = bond.GenerateBondSchedule(spotDate);
            return YieldToMarketInBase(spotDate, schedule, bond.Price.Value, bond.CouponFactor(), fxRates, bond.DayCountbasis);
        }

        public static double AccruedCoupon(this Bond bond, DateTime spotDate)
        {
            if (bond.IssueDate.HasValue && spotDate<=bond.IssueDate)
            {
                return 0;
            }
            if (bond.MaturityDate.HasValue && spotDate > bond.MaturityDate)
            {
                return 0;
            }

            var nextCouponDate = bond.NextCouponDate(spotDate);
            var prevCouponDate = nextCouponDate;

            if (bond.FirstCouponDate.HasValue && bond.IssueDate.HasValue && nextCouponDate == bond.FirstCouponDate)
            {
                prevCouponDate = bond.IssueDate.Value;
            }
            else
            {
                switch (bond.CouponFrequency)
                {
                    case CouponFrequency.SemiAnnual:
                        prevCouponDate = nextCouponDate.AddMonths(-6);
                        break;
                    case CouponFrequency.Annual:
                        prevCouponDate = nextCouponDate.AddYears(-1);
                        break;
                    case CouponFrequency.Quarterly:
                        prevCouponDate = nextCouponDate.AddMonths(-3);
                        break;
                }

                if (bond.FirstCouponDate.HasValue && prevCouponDate < bond.FirstCouponDate)
                    prevCouponDate = bond.FirstCouponDate.Value;
            }

            var daysAccrued = (spotDate - prevCouponDate).TotalDays;
            var couponPeriodDays = (nextCouponDate - prevCouponDate).TotalDays;
            if (daysAccrued > 0 && couponPeriodDays > 0)
                return bond.Coupon.Value / bond.CouponFactor() * (daysAccrued / couponPeriodDays);
            else
                return 0.0;
        }

        public static double CouponFactor(CouponFrequency f) => f switch
        {
            CouponFrequency.A => 1,
            CouponFrequency.Quarterly => 4,
            _ => 2.0,
        };

        public static double CouponFactor(this Bond bond) => CouponFactor(bond.CouponFrequency);

        public static DateTime NextCouponDate(this Bond bond, DateTime asOf)
        {
            if (!bond.MaturityDate.HasValue)
                return bond.NextCouponDatePerp(asOf);

            if (bond.FirstCouponDate.HasValue && asOf <= bond.FirstCouponDate)
                return bond.FirstCouponDate.Value;

            if(bond.FirstCouponDate.HasValue && bond.StubType == StubType.ShortBoth)
            {
                return bond.NextCouponDatePerp(asOf);
            }

            var maturity = bond.MaturityDate.Value;
            var d = maturity;
            var count = 0;
            while (d >= asOf && count < 500)
            {
                count++;
                var newD = d;
                switch (bond.CouponFrequency)
                {
                    case CouponFrequency.SemiAnnual:
                        newD = maturity.AddMonths(-6 * count);
                        break;
                    case CouponFrequency.Annual:
                        newD = maturity.AddYears(-count);
                        break;
                    case CouponFrequency.Quarterly:
                        newD = maturity.AddMonths(-3 * count);
                        break;
                }

                if (newD < asOf)
                    return d;
                d = newD;
            }

            return d;
        }

        public static DateTime NextCouponDatePerp(this Bond bond, DateTime asOf)
        {
            if (bond.FirstCouponDate.HasValue && asOf <= bond.FirstCouponDate)
            {
                return bond.FirstCouponDate.Value;
            }

            var d0 = bond.FirstCouponDate ?? bond.IssueDate ?? asOf;
            var d = d0;
            var count = 0;
            while (d < asOf && count < 500)
            {
                count++;
                var newD = d;
                switch (bond.CouponFrequency)
                {
                    case CouponFrequency.SemiAnnual:
                        newD = d0.AddMonths(6 * count);
                        break;
                    case CouponFrequency.Annual:
                        newD = d0.AddYears(count);
                        break;
                    case CouponFrequency.Quarterly:
                        newD = d0.AddMonths(3 * count);
                        break;
                }

                d = newD;
            }

            return d;
        }

        public static Dictionary<DateTime, double> GenerateBondSchedule(DateTime nextCouponDate, DateTime maturity, double coupon, double faceValue, CouponFrequency cf, double? firstCoupon = null)
        {
            var o = new Dictionary<DateTime, double>();

            var d = nextCouponDate;
            var n = 0;
            var lastCouponDate = nextCouponDate;
            var cFaq = CouponFactor(cf);

            while (d <= maturity)
            {
                var couponT = d == nextCouponDate && firstCoupon.HasValue ? firstCoupon.Value : coupon;
                o.Add(d, Convert.ToDouble(couponT / cFaq));
                n++;
                lastCouponDate = d;
                switch (cf)
                {
                    case CouponFrequency.SemiAnnual:
                        d = nextCouponDate.AddMonths(6 * n);
                        break;
                    case CouponFrequency.Annual:
                        d = nextCouponDate.AddYears(n);
                        break;
                    case CouponFrequency.Quarterly:
                        d = nextCouponDate.AddMonths(3 * n);
                        break;
                }
            }

            if (o.ContainsKey(maturity)) //final coupon paid on maturity date
            {
                o[maturity] += Convert.ToDouble(faceValue);
            }
            else
            {
                o[maturity] = Convert.ToDouble(faceValue);

                if (lastCouponDate > maturity)
                {
                    var prevCouponDate = lastCouponDate;
                    switch (cf)
                    {
                        case CouponFrequency.SemiAnnual:
                            prevCouponDate = lastCouponDate.AddMonths(-6);
                            break;
                        case CouponFrequency.Annual:
                            prevCouponDate = lastCouponDate.AddYears(-1);
                            break;
                        case CouponFrequency.Quarterly:
                            prevCouponDate = lastCouponDate.AddMonths(-3);
                            break;
                    }

                    var daysAccrued = (maturity - prevCouponDate).TotalDays;
                    var couponPeriodDays = (lastCouponDate - prevCouponDate).TotalDays;

                    if (daysAccrued > 0 && couponPeriodDays > 0)
                        o[maturity] += Convert.ToDouble(coupon / cFaq) * (daysAccrued / couponPeriodDays);

                }
                else
                {
                    var daysAccrued = (maturity - lastCouponDate).TotalDays;
                    var couponPeriodDays = (d - lastCouponDate).TotalDays;

                    if (daysAccrued > 0 && couponPeriodDays > 0)
                        o[maturity] += Convert.ToDouble(coupon / cFaq) * (daysAccrued / couponPeriodDays);
                }
            }

            return o;
        }

        public static DateTime? GetNextCallDate(this Bond bond, DateTime valDate) => bond.CallSchedule?.Where(x => x.Key > valDate).OrderBy(x => x.Key).FirstOrDefault().Key;

        public static Dictionary<DateTime, double> GenerateBondSchedule(this Bond bond, DateTime asOf)
        {
            var nextCouponDate = bond.NextCouponDate(asOf);
            return GenerateBondSchedule(nextCouponDate, bond.MaturityDate ?? bond.GetNextCallDate(asOf) ?? asOf, bond.Coupon.Value, bond.Notional, bond.CouponFrequency);
        }
    }
}
