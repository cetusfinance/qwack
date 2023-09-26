using System;
using System.Collections.Generic;
using System.Linq;
using Qwack.Core.Basic;
using Qwack.Dates;
using Qwack.Transport.BasicTypes;
using Qwack.Transport.TransportObjects.Instruments.Funding;

namespace Qwack.Core.Instruments.Funding
{
    public class GenericSwapLeg
    {
        public GenericSwapLeg() { }

        public GenericSwapLeg(DateTime startDate, DateTime endDate, Calendar calendars, Currency currency, Frequency resetFrequency, DayCountBasis dayBasis)
        {
            EffectiveDate = startDate;
            TerminationDate = new TenorDateAbsolute(endDate);
            ResetFrequency = resetFrequency;
            Currency = currency;
            SetAllCalendars(calendars);
            AccrualDCB = dayBasis;
        }

        public GenericSwapLeg(DateTime startDate, Frequency tenor, Calendar calendars, Currency currency, Frequency resetFrequency, DayCountBasis dayBasis)
        {
            EffectiveDate = startDate;
            TerminationDate = new TenorDateRelative(tenor);
            ResetFrequency = resetFrequency;
            Currency = currency;
            SetAllCalendars(calendars);
            AccrualDCB = dayBasis;
        }

        public GenericSwapLeg(TO_GenericSwapLeg to, ICalendarProvider calendarProvider, ICurrencyProvider currencyProvider)
        {
            EffectiveDate = to.EffectiveDate;
            TerminationDate = to.TerminationDate.Relative != null ? new TenorDateRelative(new Frequency(to.TerminationDate.Relative)) : new TenorDateAbsolute(to.TerminationDate.Absolute??DateTime.MinValue);
            ResetFrequency = new Frequency(to.ResetFrequency);
            Currency = currencyProvider.GetCurrencySafe(to.Currency);
            AccrualDCB = to.AccrualDCB;
            FixingCalendar = calendarProvider.GetCalendarSafe(to.FixingCalendar);
            ResetCalendar = calendarProvider.GetCalendarSafe(to.ResetCalendar);
            AccrualCalendar = calendarProvider.GetCalendarSafe(to.AccrualCalendar);
            PaymentCalendar = calendarProvider.GetCalendarSafe(to.PaymentCalendar);
            ResetRollType = to.ResetRollType;
            PaymentRollType = to.PaymentRollType;
            FixingRollType = to.FixingRollType;
            RollDay = to.RollDay;
            StubType = to.StubType;
            ResetFrequency = new Frequency(to.ResetFrequency);
            FixingOffset = new Frequency(to.FixingOffset);
            ForecastTenor = new Frequency(to.ForecastTenor);
            LegType = to.LegType;
            PaymentOffset = new Frequency(to.PaymentOffset);
            PaymentOffsetRelativeTo = to.PaymentOffsetRelativeTo;
            FixedRateOrMargin = to.FixedRateOrMargin;
            Nominal = to.Nominal;
            FraDiscounting = to.FraDiscounting;
            AveragingType = to.AveragingType;
            NotionalExchange = to.NotionalExchange;
            Direction = to.Direction;
            TrsLegType = to.TrsLegType;
            AssetId = to.AssetId;
        }

        private void SetAllCalendars(Calendar calendars)
        {
            FixingCalendar = calendars;
            AccrualCalendar = calendars;
            ResetCalendar = calendars;
            PaymentCalendar = calendars;
        }

        public Currency Currency { get; set; }
        public DateTime EffectiveDate { get; set; }
        public ITenorDate TerminationDate { get; set; }
        public Calendar FixingCalendar { get; set; }
        public Calendar ResetCalendar { get; set; }
        public Calendar AccrualCalendar { get; set; }
        public Calendar PaymentCalendar { get; set; }
        public RollType ResetRollType { get; set; } = RollType.ModFollowing;
        public RollType PaymentRollType { get; set; } = RollType.Following;
        public RollType FixingRollType { get; set; } = RollType.Previous;
        public string RollDay { get; set; } = "Termination";
        public StubType StubType { get; set; } = StubType.ShortFront;
        public Frequency ResetFrequency { get; set; }
        public Frequency FixingOffset { get; set; } = new Frequency("2b");
        public Frequency ForecastTenor { get; set; }
        public SwapLegType LegType { get; set; }
        public Frequency PaymentOffset { get; set; } = 0.Bd();
        public OffsetRelativeToType PaymentOffsetRelativeTo { get; set; } = OffsetRelativeToType.PeriodEnd;
        public decimal FixedRateOrMargin { get; set; }
        public decimal Nominal { get; set; } = 1e6M;
        public DayCountBasis AccrualDCB { get; set; }
        public FraDiscountingType FraDiscounting { get; set; }
        public AverageType AveragingType { get; set; }
        public ExchangeType NotionalExchange { get; set; }
        public SwapPayReceiveType Direction { get; set; }
        public TrsLegType? TrsLegType { get; set; }
        public string AssetId { get; set; }

        public GenericSwapLeg Clone() => new()
        {
            AccrualCalendar = AccrualCalendar,
            AccrualDCB = AccrualDCB,
            AveragingType = AveragingType,
            Currency = Currency,
            Direction = Direction,
            EffectiveDate = EffectiveDate,
            FixedRateOrMargin = FixedRateOrMargin,
            FixingCalendar = FixingCalendar,
            FixingOffset = FixingOffset,
            FixingRollType = FixingRollType,
            ForecastTenor = ForecastTenor,
            FraDiscounting = FraDiscounting,
            LegType = LegType,
            Nominal = Nominal,
            NotionalExchange = NotionalExchange,
            PaymentCalendar = PaymentCalendar,
            PaymentOffset = PaymentOffset,
            PaymentOffsetRelativeTo = PaymentOffsetRelativeTo,
            PaymentRollType = PaymentRollType,
            ResetCalendar = ResetCalendar,
            ResetFrequency = ResetFrequency,
            ResetRollType = ResetRollType,
            RollDay = RollDay,
            StubType = StubType,
            TerminationDate = TerminationDate,
            TrsLegType = TrsLegType,
            AssetId = AssetId,
        };

        public TO_GenericSwapLeg GetTransportObject() => new()
        {
            EffectiveDate = EffectiveDate,
            TerminationDate = new TO_ITenorDate { Absolute = TerminationDate is TenorDateAbsolute ta ? ta.AbsoluteDate : null, Relative = TerminationDate is TenorDateRelative tr ? tr.RelativeTenor.ToString() : null },
            ResetFrequency = ResetFrequency.ToString(),
            Currency = Currency.ToString(),
            AccrualDCB = AccrualDCB,
            FixingCalendar = FixingCalendar.Name,
            ResetCalendar = ResetCalendar.Name,
            AccrualCalendar = AccrualCalendar.Name,
            PaymentCalendar = PaymentCalendar.Name,
            ResetRollType = ResetRollType,
            PaymentRollType = PaymentRollType,
            FixingRollType = FixingRollType,
            RollDay = RollDay,
            StubType = StubType,
            FixingOffset = FixingOffset.ToString(),
            ForecastTenor = ForecastTenor.ToString(),
            LegType = LegType,
            PaymentOffset = PaymentOffset.ToString(),
            PaymentOffsetRelativeTo = PaymentOffsetRelativeTo,
            FixedRateOrMargin = FixedRateOrMargin,
            Nominal = Nominal,
            FraDiscounting = FraDiscounting,
            AveragingType = AveragingType,
            NotionalExchange = NotionalExchange,
            Direction = Direction,
            TrsLegType = TrsLegType,
            AssetId = AssetId,
        };
    

        public CashFlowSchedule GenerateSchedule()
        {
            var startDate = EffectiveDate;
            var endDate = TerminationDate.Date(startDate, ResetRollType, ResetCalendar);
            var f = new CashFlowSchedule();
            var lf = new List<CashFlow>();

            if(LegType==SwapLegType.InflationCoupon)
            {
                var yf = startDate.CalculateYearFraction(endDate, AccrualDCB);
                lf.Add(new CashFlow
                {
                    Notional = -(double)Nominal * (Direction == SwapPayReceiveType.Payer ? -1.0 : 1.0),
                    Fv = -(double)Nominal * (Direction == SwapPayReceiveType.Payer ? -1.0 : 1.0),
                    SettleDate = endDate,
                    AccrualPeriodEnd = endDate,
                    AccrualPeriodStart = startDate,
                    YearFraction = yf,
                    Dcf = yf,
                    FlowType = FlowType.FloatInflation,
                    FixedRateOrMargin = (double)FixedRateOrMargin,
                    Currency = Currency,
                });
                f.Flows = lf;
                return f;
            }

            if(LegType==SwapLegType.AssetPerformance && TrsLegType == Transport.BasicTypes.TrsLegType.Bullet)
            {
                var yf = startDate.CalculateYearFraction(endDate, AccrualDCB);
                lf.Add(new CashFlow
                {
                    Notional = -(double)Nominal * (Direction == SwapPayReceiveType.Payer ? -1.0 : 1.0),
                    Fv = -(double)Nominal * (Direction == SwapPayReceiveType.Payer ? -1.0 : 1.0),
                    SettleDate = endDate,
                    AccrualPeriodEnd = endDate,
                    AccrualPeriodStart = startDate,
                    YearFraction = yf,
                    Dcf = 1,
                    FlowType = FlowType.AssetPerformance,
                    FixedRateOrMargin = 0,
                    AssetId = AssetId,
                    Currency = Currency,
                });
                f.Flows = lf;
                return f;
            }

            if (NotionalExchange is ExchangeType.FrontOnly or ExchangeType.Both)
            {
                lf.Add(new CashFlow
                {
                    Notional = (double)Nominal * (Direction == SwapPayReceiveType.Payer ? -1.0 : 1.0),
                    Fv = (double)Nominal * (Direction == SwapPayReceiveType.Payer ? -1.0 : 1.0),
                    SettleDate = startDate,
                    YearFraction = 1.0,
                    Dcf = 1.0,
                    FlowType = FlowType.FixedAmount
                });
            }

            //need to handle stub types and roll day types
            switch (StubType)
            {
                case StubType.ShortFront:
                case StubType.LongFront:
                    {
                        var nQ = 0;
                        var currentReset = GetNextResetDate(endDate, false);
                        while (GetNextResetDate(currentReset, false) >= startDate)
                        {
                            var q = new CashFlow()
                            {
                                ResetDateStart = currentReset,
                                AccrualPeriodStart = currentReset,
                                FixingDateStart = currentReset.SubtractPeriod(FixingRollType, FixingCalendar, FixingOffset),
                                AccrualPeriodEnd = currentReset.AddPeriod(ResetRollType, ResetCalendar, ResetFrequency)
                            };
                            q.SettleDate = (PaymentOffsetRelativeTo == OffsetRelativeToType.PeriodEnd) ?
                                q.AccrualPeriodEnd.AddPeriod(PaymentRollType, PaymentCalendar, PaymentOffset) :
                                q.AccrualPeriodStart.AddPeriod(PaymentRollType, PaymentCalendar, PaymentOffset);
                            q.YearFraction = (LegType is not SwapLegType.FixedNoAccrual and not SwapLegType.FloatNoAccrual) ?
                                 q.AccrualPeriodStart.CalculateYearFraction(q.AccrualPeriodEnd, AccrualDCB) :
                                 1.0;
                            q.Dcf = q.YearFraction;
                            q.Fv = (LegType == SwapLegType.Fixed) ?
                                (double)Nominal * q.YearFraction * (double)FixedRateOrMargin :
                                0;
                            q.FixedRateOrMargin = (double)FixedRateOrMargin;
                            q.FlowType = (LegType == SwapLegType.Fixed) ? FlowType.FixedRate : FlowType.FloatRate;
                            q.Notional = (double)Nominal;
                            lf.Add(q);
                            nQ++;
                            currentReset = GetNextResetDate(currentReset, false);
                        }

                        if (lf.Count == 0 || lf.Last().AccrualPeriodStart != startDate)
                        {
                            if (StubType == StubType.LongFront)
                            {
                                var Q = lf.Last();
                                Q.ResetDateStart = startDate;
                                Q.AccrualPeriodStart = startDate;
                                Q.SettleDate = (PaymentOffsetRelativeTo == OffsetRelativeToType.PeriodEnd) ?
                                    Q.AccrualPeriodEnd.AddPeriod(PaymentRollType, PaymentCalendar, PaymentOffset) :
                                    Q.AccrualPeriodStart.AddPeriod(PaymentRollType, PaymentCalendar, PaymentOffset);
                                Q.Dcf = (LegType is not SwapLegType.FixedNoAccrual and not SwapLegType.FloatNoAccrual) ?
                                    Q.AccrualPeriodStart.CalculateYearFraction(Q.AccrualPeriodEnd, AccrualDCB) :
                                    1.0;
                            }
                            else
                            {
                                var q = new CashFlow()
                                {
                                    AccrualPeriodStart = startDate,
                                    FixingDateStart = startDate.SubtractPeriod(FixingRollType, FixingCalendar, FixingOffset),
                                    AccrualPeriodEnd = (lf.Count > 0 && lf.Last().AccrualPeriodEnd != DateTime.MinValue) ? lf.Last().AccrualPeriodStart : endDate
                                };

                                q.SettleDate = (PaymentOffsetRelativeTo == OffsetRelativeToType.PeriodEnd) ?
                                    q.AccrualPeriodEnd.AddPeriod(PaymentRollType, PaymentCalendar, PaymentOffset) :
                                    q.AccrualPeriodStart.AddPeriod(PaymentRollType, PaymentCalendar, PaymentOffset);
                                //Q.Currency = CCY;
                                q.YearFraction = (LegType is not SwapLegType.FixedNoAccrual and not SwapLegType.FloatNoAccrual) ?
                                 q.AccrualPeriodStart.CalculateYearFraction(q.AccrualPeriodEnd, AccrualDCB) :
                                 1.0;
                                q.Dcf = q.YearFraction;
                                q.Fv = (LegType == SwapLegType.Fixed) ?
                                    (double)Nominal * q.YearFraction * (double)FixedRateOrMargin :
                                    0;
                                q.FixedRateOrMargin = (double)FixedRateOrMargin;
                                q.FlowType = (LegType == SwapLegType.Fixed) ? FlowType.FixedRate : FlowType.FloatRate;
                                q.Notional = (double)Nominal;
                                lf.Add(q);
                                nQ++;
                            }
                        }


                        break;
                    }
                case StubType.ShortBack:
                case StubType.LongBack:
                    {
                        var nQ = 0;
                        var currentReset = startDate;
                        while (GetNextResetDate(currentReset, true) <= endDate)
                        {
                            var Q = new CashFlow()
                            {
                                AccrualPeriodStart = currentReset,
                                FixingDateStart = currentReset.SubtractPeriod(FixingRollType, FixingCalendar, FixingOffset),
                                AccrualPeriodEnd = currentReset.AddPeriod(ResetRollType, ResetCalendar, ResetFrequency)
                            };
                            Q.SettleDate = (PaymentOffsetRelativeTo == OffsetRelativeToType.PeriodEnd) ?
                                Q.AccrualPeriodEnd.AddPeriod(PaymentRollType, PaymentCalendar, PaymentOffset) :
                                Q.AccrualPeriodStart.AddPeriod(PaymentRollType, PaymentCalendar, PaymentOffset);
                            //Q.Currency = CCY;
                            Q.YearFraction = (LegType is not SwapLegType.FixedNoAccrual and not SwapLegType.FloatNoAccrual) ?
                                Q.AccrualPeriodStart.CalculateYearFraction(Q.AccrualPeriodEnd, AccrualDCB) :
                                1.0;
                            Q.Dcf = Q.YearFraction;
                            Q.Notional = (double)Nominal;
                            Q.Fv = (LegType == SwapLegType.Fixed) ?
                                (double)Nominal * Q.YearFraction * (double)FixedRateOrMargin :
                                0;
                            Q.FixedRateOrMargin = (double)FixedRateOrMargin;
                            Q.FlowType = (LegType == SwapLegType.Fixed) ? FlowType.FixedRate : FlowType.FloatRate;
                            lf.Add(Q);
                            nQ++;
                            currentReset = GetNextResetDate(currentReset, false);
                        }



                        if (lf.Last().AccrualPeriodEnd != endDate)
                        {
                            if (StubType == StubType.LongBack)
                            {
                                var Q = lf.Last();
                                Q.AccrualPeriodEnd = endDate;
                                Q.SettleDate = (PaymentOffsetRelativeTo == OffsetRelativeToType.PeriodEnd) ?
                                    Q.AccrualPeriodEnd.AddPeriod(PaymentRollType, PaymentCalendar, PaymentOffset) :
                                    Q.AccrualPeriodStart.AddPeriod(PaymentRollType, PaymentCalendar, PaymentOffset);
                                Q.Dcf = (LegType is not SwapLegType.FixedNoAccrual and not SwapLegType.FloatNoAccrual) ?
                                    Q.AccrualPeriodStart.CalculateYearFraction(Q.AccrualPeriodEnd, AccrualDCB) :
                                    1.0;
                            }
                            else
                            {
                                var Q = new CashFlow()
                                {
                                    AccrualPeriodStart = lf.Last().AccrualPeriodEnd,
                                    FixingDateStart = startDate.SubtractPeriod(FixingRollType, FixingCalendar, FixingOffset),
                                    AccrualPeriodEnd = endDate
                                };
                                Q.SettleDate = (PaymentOffsetRelativeTo == OffsetRelativeToType.PeriodEnd) ?
                                    Q.AccrualPeriodEnd.AddPeriod(PaymentRollType, PaymentCalendar, PaymentOffset) :
                                    Q.AccrualPeriodStart.AddPeriod(PaymentRollType, PaymentCalendar, PaymentOffset);
                                //Q.Currency = CCY;
                                Q.YearFraction = (LegType is not SwapLegType.FixedNoAccrual and not SwapLegType.FloatNoAccrual) ?
                                   Q.AccrualPeriodStart.CalculateYearFraction(Q.AccrualPeriodEnd, AccrualDCB) :
                                   1.0; Q.Notional = (double)Nominal;
                                Q.Dcf = Q.YearFraction;
                                Q.Fv = (LegType == SwapLegType.Fixed) ?
                                    (double)Nominal * Q.YearFraction * (double)FixedRateOrMargin :
                                    0;
                                Q.FixedRateOrMargin = (double)FixedRateOrMargin;
                                Q.FlowType = (LegType == SwapLegType.Fixed) ? FlowType.FixedRate : FlowType.FloatRate;
                                Q.Notional = (double)Nominal;
                                lf.Add(Q);
                                nQ++;
                            }
                        }
                        break;
                    }
                case StubType.LongBoth:
                case StubType.ShortBoth:
                    throw new NotImplementedException("Schedules with Both type stubs cannot be generated");
            }

            if (NotionalExchange is ExchangeType.BackOnly or ExchangeType.Both)
            {
                lf.Add(new CashFlow
                {
                    Notional = (double)Nominal * (Direction == SwapPayReceiveType.Receiver ? -1.0 : 1.0),
                    Fv = (double)Nominal * (Direction == SwapPayReceiveType.Receiver ? -1.0 : 1.0),
                    SettleDate = endDate,
                    YearFraction = 1.0,
                    Dcf = 1.0,
                    FlowType = FlowType.FixedAmount
                });
            }
            f.Flows = lf.OrderBy(x => x.AccrualPeriodStart).ToList();

            return f;
        }

        private DateTime GetNextResetDate(DateTime currentReset, bool fwdDirection)
        {
            if (RollDay == "IMM")
                return fwdDirection ? currentReset.GetNextImmDate() : currentReset.GetPrevImmDate();
            if (RollDay == "EOM")
                if (fwdDirection)
                {
                    var d1 = currentReset.AddPeriod(ResetRollType, ResetCalendar, ResetFrequency);
                    return d1.LastDayOfMonth().AddPeriod(RollType.P, ResetCalendar, 0.Bd());
                }
                else
                {
                    var d1 = currentReset.SubtractPeriod(ResetRollType, ResetCalendar, ResetFrequency);
                    return d1.LastDayOfMonth().AddPeriod(RollType.P, ResetCalendar, 0.Bd());
                }

            if (int.TryParse(RollDay, out var rollOut))
                if (fwdDirection)
                {
                    var d1 = currentReset.AddPeriod(ResetRollType, ResetCalendar, ResetFrequency);
                    return new DateTime(d1.Year, d1.Month, rollOut).AddPeriod(ResetRollType, ResetCalendar, 0.Bd());
                }
                else
                {
                    var d1 = currentReset.SubtractPeriod(ResetRollType, ResetCalendar, ResetFrequency);
                    return new DateTime(d1.Year, d1.Month, rollOut).AddPeriod(ResetRollType, ResetCalendar, 0.Bd());
                }
            return fwdDirection ? currentReset.AddPeriod(ResetRollType, ResetCalendar, ResetFrequency) : currentReset.SubtractPeriod(FlipRoll(ResetRollType), ResetCalendar, ResetFrequency);
        }

        private static RollType FlipRoll(RollType rollType) => rollType switch
        {
            RollType.Following => RollType.Previous,
            RollType.ModFollowing => RollType.ModPrevious,
            _ => rollType,
        };
    }
}
