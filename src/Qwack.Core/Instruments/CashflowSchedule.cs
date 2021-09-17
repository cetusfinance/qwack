using System;
using System.Collections.Generic;
using System.Linq;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Core.Models;
using Qwack.Transport.BasicTypes;

namespace Qwack.Core.Instruments
{
    public class CashFlowSchedule
    {
        public List<CashFlow> Flows { get; set; }
        public DayCountBasis DayCountBasis { get; set; }
        public ResetType ResetType { get; set; }
        public AverageType AverageType { get; set; }

        public CashFlowSchedule Clone() => new()
        {
            Flows = new List<CashFlow>(Flows.Select(x => x.Clone())),
            DayCountBasis = DayCountBasis,
            ResetType = ResetType,
            AverageType = AverageType
        };
    }

    public static class CashFlowScheduleEx
    {
        public static double PV(this CashFlowSchedule schedule, IrCurve discountCurve, IrCurve forecastCurve, bool updateState, bool updateDf, bool updateEstimate, DayCountBasis basisFloat, DateTime? filterDate)
        {
            double totalPv = 0;

            for (var i = 0; i < schedule.Flows.Count; i++)
            {
                var flow = schedule.Flows[i];

                if (filterDate.HasValue && flow.SettleDate < filterDate.Value)
                    continue;

                double fv, pv, df;

                switch (flow.FlowType)
                {
                    case FlowType.FixedRate:
                        {
                            if (updateState)
                            {
                                var rateLin = flow.FixedRateOrMargin;
                                var yf = flow.YearFraction;
                                fv = rateLin * yf * flow.Notional;
                            }
                            else
                            {
                                fv = flow.Fv;
                            }

                            if (updateDf)
                            {
                                df = discountCurve.Pv(1, flow.SettleDate);
                            }
                            else
                            {
                                df = flow.Fv == flow.Pv ? 1.0 : flow.Pv / flow.Fv;
                            }

                            pv = fv * df;

                            totalPv += pv;

                            if (updateState)
                            {
                                flow.Fv = fv;
                                flow.Pv = pv;
                            }
                            break;
                        }
                    case FlowType.FloatRate:
                        {
                            if (updateEstimate)
                            {
                                var s = flow.AccrualPeriodStart;
                                var e = flow.AccrualPeriodEnd;
                                var rateLin = forecastCurve.GetForwardRate(s, e, RateType.Linear, basisFloat);
                                rateLin += flow.FixedRateOrMargin;
                                var yf = flow.YearFraction;
                                fv = rateLin * yf * flow.Notional;
                            }
                            else
                            {
                                fv = flow.Fv;
                            }

                            if (updateDf)
                                df = discountCurve.Pv(1, flow.SettleDate);
                            else
                                df = flow.Fv == flow.Pv ? 1.0 : flow.Pv / flow.Fv;

                            pv = fv * df;
                            totalPv += pv;

                            if (updateState)
                            {
                                flow.Fv = fv;
                                flow.Pv = pv;
                            }
                            break;
                        }
                    case FlowType.FixedAmount:
                        {
                            fv = flow.Notional;

                            if (updateDf)
                                df = discountCurve.Pv(1, flow.SettleDate);
                            else
                                df = flow.Fv == flow.Pv ? 1.0 : flow.Pv / flow.Fv;

                            pv = fv * df;
                            totalPv += pv;

                            if (updateState)
                            {
                                flow.Fv = fv;
                                flow.Pv = pv;
                            }

                            break;
                        }
                }

            }

            return totalPv;
        }

        public static double PV(this CashFlowSchedule schedule, Currency reportingCCy, IFundingModel model, string forecastCurve, DayCountBasis basisFloat, DateTime? filterDate)
        {
            var totalPv = 0.0;
            for (var i = 0; i < schedule.Flows.Count; i++)
            {
                var flow = schedule.Flows[i];

                if (filterDate.HasValue && flow.SettleDate < filterDate.Value)
                    continue;

                double fv, pv;
                var df = model.GetDf(reportingCCy, model.BuildDate, flow.SettleDate);
                var fwdFxRate = model.GetFxRate(flow.SettleDate, flow.Currency, reportingCCy);
                
                switch (flow.FlowType)
                {
                    case FlowType.FixedRate:
                        {
                            var rateLin = flow.FixedRateOrMargin;
                            var yf = flow.YearFraction;
                            fv = rateLin * yf * flow.Notional;
                            fv *= fwdFxRate;
                            pv = fv * df;
                            totalPv += pv;
                            break;
                        }
                    case FlowType.FloatRate:
                        {
                            var s = flow.AccrualPeriodStart;
                            var e = flow.AccrualPeriodEnd;
                            var rateLin = model.GetCurve(forecastCurve).GetForwardRate(s, e, RateType.Linear, basisFloat);
                            rateLin += flow.FixedRateOrMargin;
                            var yf = flow.YearFraction;
                            fv = rateLin * yf * flow.Notional;
                            fv *= fwdFxRate;
                            pv = fv * df;
                            totalPv += pv;
                            break;
                        }
                    case FlowType.FixedAmount:
                        {
                            fv = flow.Notional;
                            fv *= fwdFxRate;
                            pv = fv * df;
                            totalPv += pv;
                            break;
                        }
                }

            }

            return totalPv;
        }
    }
}
