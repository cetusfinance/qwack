using System;
using System.Collections.Generic;
using System.Linq;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Transport.BasicTypes;
using Qwack.Transport.TransportObjects.Instruments.Funding;

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

        public CashFlowSchedule() { }

        public CashFlowSchedule(TO_CashFlowSchedule to, ICalendarProvider calendarProvider, ICurrencyProvider currencyProvider)
        {
            Flows = to.Flows.Select(x => new CashFlow(x,calendarProvider,currencyProvider)).ToList();
            DayCountBasis = to.DayCountBasis;
            ResetType = to.ResetType;
            AverageType = to.AverageType;
        }

        public TO_CashFlowSchedule GetTransportObject() => new TO_CashFlowSchedule
        {
            AverageType = AverageType,
            DayCountBasis = DayCountBasis,
            ResetType = ResetType,
            Flows = Flows.Select(f => f.GetTransportObject()).ToList()
        };
    }

    public static class CashFlowScheduleEx
    {
        public static double PV(this CashFlowSchedule schedule, IIrCurve discountCurve, IIrCurve forecastCurve, bool updateState, bool updateDf, bool updateEstimate, DayCountBasis basisFloat, DateTime? filterDate, double? initialCpiFixing = null)
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
                                df = discountCurve.GetDf(discountCurve.BuildDate, flow.SettleDate);
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
                                df = discountCurve.GetDf(discountCurve.BuildDate, flow.SettleDate);
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
                                df = discountCurve.GetDf(discountCurve.BuildDate, flow.SettleDate);
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
                    case FlowType.FloatInflation:
                        {
                            if (updateEstimate)
                            {
                                if (forecastCurve is not CPICurve infCurve)
                                {
                                    throw new Exception("Curve is not inflation type");
                                }
                                var s = flow.AccrualPeriodStart;
                                var e = flow.AccrualPeriodEnd;
                                var cpiStart = initialCpiFixing ?? forecastCurve.GetRate(s);
                                var cpiEnd =  infCurve.GetForecast(e, flow.CpiFixingLagInMonths);
                                fv = cpiEnd / cpiStart * flow.Notional * flow.FixedRateOrMargin * flow.YearFraction; 
                            }
                            else
                            {
                                fv = flow.Fv;
                            }

                            if (updateDf)
                                df = discountCurve.GetDf(discountCurve.BuildDate, flow.SettleDate);
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
        public static double PV(this CashFlowSchedule schedule, Currency reportingCCy, IAssetFxModel model, DateTime? filterDate)
        {
            var totalPv = 0.0;
            for (var i = 0; i < schedule.Flows.Count; i++)
            {
                var flow = schedule.Flows[i];

                if (string.IsNullOrEmpty(flow.AssetId))
                    continue;
                if (filterDate.HasValue && flow.SettleDate < filterDate.Value)
                    continue;

                double fv, pv;
                var df = model.FundingModel.GetDf(reportingCCy, model.BuildDate, flow.SettleDate);
                var fwdFxRate = model.FundingModel.GetFxRate(flow.SettleDate, flow.Currency, reportingCCy);
                var curve = model.GetPriceCurve(flow.AssetId);
                if (!model.TryGetFixingDictionary(flow.AssetId, out var assetFixings))
                    assetFixings = new FixingDictionary(new Dictionary<DateTime, double>());

                switch (flow.FlowType)
                {
                    case FlowType.AssetNotional:
                        {
                            var price = curve.GetPriceForFixingDate(flow.AccrualPeriodEnd);
                            var rateLin = flow.FixedRateOrMargin;
                            var yf = flow.YearFraction;
                            fv = rateLin * yf * flow.Notional * price;
                            fv *= fwdFxRate;
                            pv = fv * df;
                            totalPv += pv;
                            break;
                        }
                    case FlowType.AssetPerformance:
                        {
                            var s = flow.AccrualPeriodStart;
                            var e = flow.AccrualPeriodEnd;
                            var priceS = flow.InitialFixing ?? (assetFixings.TryGetValue(s, out var fs) ? fs : curve.GetPriceForFixingDate(s));
                            var priceE = assetFixings.TryGetValue(e, out var fe) ? fe : curve.GetPriceForFixingDate(e);
                            var rateLin = priceE / priceS - 1.0;
                            rateLin += flow.FixedRateOrMargin;
                            var yf = flow.YearFraction;
                            fv = rateLin * yf * flow.Notional;
                            fv *= fwdFxRate;
                            pv = fv * df;
                            totalPv += pv;
                            break;
                        }
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


        public static double FlowsT0(this CashFlowSchedule schedule, Currency reportingCCy, IFundingModel model, string forecastCurve, DayCountBasis basisFloat, DateTime filterDate)
        {
            var totalPv = 0.0;
            for (var i = 0; i < schedule.Flows.Count; i++)
            {
                var flow = schedule.Flows[i];

                if (flow.SettleDate != filterDate)
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

        public static double FlowsT0(this CashFlowSchedule schedule, Currency reportingCCy, IAssetFxModel model, DateTime filterDate)
        {
            var totalPv = 0.0;
            for (var i = 0; i < schedule.Flows.Count; i++)
            {
                var flow = schedule.Flows[i];

                if (string.IsNullOrEmpty(flow.AssetId))
                    continue;
                if (flow.SettleDate != filterDate)
                    continue;

                double fv, pv;
                var df = model.FundingModel.GetDf(reportingCCy, model.BuildDate, flow.SettleDate);
                var fwdFxRate = model.FundingModel.GetFxRate(flow.SettleDate, flow.Currency, reportingCCy);
                var curve = model.GetPriceCurve(flow.AssetId);
                if (!model.TryGetFixingDictionary(flow.AssetId, out var assetFixings))
                    assetFixings = new FixingDictionary(new Dictionary<DateTime, double>());

                switch (flow.FlowType)
                {
                    case FlowType.AssetNotional:
                        {
                            var price = curve.GetPriceForFixingDate(flow.AccrualPeriodEnd);
                            var rateLin = flow.FixedRateOrMargin;
                            var yf = flow.YearFraction;
                            fv = rateLin * yf * flow.Notional * price;
                            fv *= fwdFxRate;
                            pv = fv * df;
                            totalPv += pv;
                            break;
                        }
                    case FlowType.AssetPerformance:
                        {
                            var s = flow.AccrualPeriodStart;
                            var e = flow.AccrualPeriodEnd;
                            var priceS = flow.InitialFixing ?? (assetFixings.TryGetValue(s, out var fs) ? fs : curve.GetPriceForFixingDate(s));
                            var priceE = assetFixings.TryGetValue(e, out var fe) ? fe : curve.GetPriceForFixingDate(e);
                            var rateLin = priceE / priceS - 1.0;
                            rateLin += flow.FixedRateOrMargin;
                            var yf = flow.YearFraction;
                            fv = rateLin * yf * flow.Notional;
                            fv *= fwdFxRate;
                            pv = fv * df;
                            totalPv += pv;
                            break;
                        }
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

        public static CashFlow[] ExpectedFlows(this CashFlowSchedule schedule, Currency reportingCCy, IFundingModel model, string forecastCurve, DayCountBasis basisFloat, DateTime? filterDate)
        {
            List<CashFlow> flows = new(); ;
            for (var i = 0; i < schedule.Flows.Count; i++)
            {
                var flow = schedule.Flows[i].Clone();

                if (filterDate.HasValue && flow.SettleDate != filterDate)
                    continue;

                flows.Add(flow);
                var df = model.GetDf(reportingCCy, model.BuildDate, flow.SettleDate);
                var fwdFxRate = model.GetFxRate(flow.SettleDate, flow.Currency, reportingCCy);

                switch (flow.FlowType)
                {
                    case FlowType.FixedRate:
                        {
                            var rateLin = flow.FixedRateOrMargin;
                            var yf = flow.YearFraction;
                            flow.Fv = rateLin * yf * flow.Notional;
                            flow.Fv *= fwdFxRate;
                            flow.Pv = flow.Fv * df;
                            break;
                        }
                    case FlowType.FloatRate:
                        {
                            var s = flow.AccrualPeriodStart;
                            var e = flow.AccrualPeriodEnd;
                            var rateLin = model.GetCurve(forecastCurve).GetForwardRate(s, e, RateType.Linear, basisFloat);
                            rateLin += flow.FixedRateOrMargin;
                            var yf = flow.YearFraction;
                            flow.Fv = rateLin * yf * flow.Notional;
                            flow.Fv *= fwdFxRate;
                            flow.Pv = flow.Fv * df;
                            break;
                        }
                    case FlowType.FixedAmount:
                        {
                            flow.Fv = flow.Notional * fwdFxRate;
                            flow.Pv = flow.Fv * df;
                            break;
                        }
                }

            }

            return flows.ToArray();
        }

        public static CashFlow[] ExpectedFlows(this CashFlowSchedule schedule, Currency reportingCCy, IAssetFxModel model, DateTime? filterDate)
        {
            var flows = new List<CashFlow>();
            for (var i = 0; i < schedule.Flows.Count; i++)
            {
                var flow = schedule.Flows[i].Clone();
                flows.Add(flow);
                if (string.IsNullOrEmpty(flow.AssetId))
                    continue;
                if (filterDate.HasValue && flow.SettleDate != filterDate)
                    continue;

                var df = model.FundingModel.GetDf(reportingCCy, model.BuildDate, flow.SettleDate);
                var fwdFxRate = model.FundingModel.GetFxRate(flow.SettleDate, flow.Currency, reportingCCy);
                var curve = model.GetPriceCurve(flow.AssetId);
                if (!model.TryGetFixingDictionary(flow.AssetId, out var assetFixings))
                    assetFixings = new FixingDictionary(new Dictionary<DateTime, double>());

                switch (flow.FlowType)
                {
                    case FlowType.AssetNotional:
                        {
                            var price = curve.GetPriceForFixingDate(flow.AccrualPeriodEnd);
                            var rateLin = flow.FixedRateOrMargin;
                            var yf = flow.YearFraction;
                            flow.Fv = rateLin * yf * flow.Notional * price;
                            flow.Fv *= fwdFxRate;
                            flow.Pv = flow.Fv * df;
                            break;
                        }
                    case FlowType.AssetPerformance:
                        {
                            var s = flow.AccrualPeriodStart;
                            var e = flow.AccrualPeriodEnd;
                            var priceS = flow.InitialFixing ?? (assetFixings.TryGetValue(s, out var fs) ? fs : curve.GetPriceForFixingDate(s));
                            var priceE = assetFixings.TryGetValue(e, out var fe) ? fe : curve.GetPriceForFixingDate(e);
                            var rateLin = priceE / priceS - 1.0;
                            rateLin += flow.FixedRateOrMargin;
                            var yf = flow.YearFraction;
                            flow.Fv = rateLin * flow.Notional;
                            flow.Fv *= fwdFxRate;
                            flow.Pv = flow.Fv * df;
                            break;
                        }
                    case FlowType.FixedRate:
                        {
                            var rateLin = flow.FixedRateOrMargin;
                            var yf = flow.YearFraction;
                            flow.Fv = rateLin * yf * flow.Notional;
                            flow.Fv *= fwdFxRate;
                            flow.Pv = flow.Fv * df;
                            break;
                        }
                    case FlowType.FixedAmount:
                        {
                            flow.Fv = flow.Notional;
                            flow.Fv *= fwdFxRate;
                            flow.Pv = flow.Fv * df;
                            break;
                        }
                }

            }

            return flows.ToArray();
        }

    }
}
