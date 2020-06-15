using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Core.Instruments.Funding;
using Qwack.Dates;
using Qwack.Math;
using Qwack.Math.Interpolation;
using Qwack.Transport.BasicTypes;

namespace Qwack.Core.Instruments.Credit
{
    public class CDS
    {
        public DateTime OriginDate { get; set; }
        public CdsScheduleType ScheduleType { get; set; }
        public DayCountBasis Basis { get; set; }
        public double Spread { get; set; }
        public Frequency Tenor { get; set; }
        public Calendar HolidayCalendar { get; set; }
        public Currency Currency { get; set; }
        public string DiscountCurve { get; set; }
        public double Notional { get; set; }

        public GenericSwapLeg FixedLeg { get; set; }
        public CashFlowSchedule FixedSchedule { get; set; }

        public DateTime FinalSensitivityDate => FixedSchedule.Flows.Max(f => f.AccrualPeriodEnd);

        public CDS()
        {
            
        }

        public void Init()
        {
            Frequency resetFrequency;
            string rollDay;
            switch(ScheduleType)
            {
                case CdsScheduleType.Basic:
                    resetFrequency = new Frequency(3, DatePeriodType.M);
                    rollDay = "Termination";
                    break;
                case CdsScheduleType.DualStubs:
                case CdsScheduleType.Imm3M:
                    resetFrequency = new Frequency(3, DatePeriodType.M);
                    rollDay = "IMM";
                    break;
                case CdsScheduleType.Imm6M:
                    resetFrequency = new Frequency(6, DatePeriodType.M);
                    rollDay = "IMM";
                    break;
                default:
                    throw new Exception("Unable to handle CDS schedule type");
            }
            FixedLeg = new GenericSwapLeg(OriginDate, Tenor, HolidayCalendar, Currency, resetFrequency, Basis)
            {
                RollDay = rollDay,
                Nominal = (decimal)Notional
            };
            FixedSchedule = FixedLeg.GenerateSchedule();
        }

        //http://www.bnikolic.co.uk/cds/cdsvaluation.html 
        //var contingentLeg = (1.0 - recoveryRate) *
        public double PV_PiecewiseFlat(HazzardCurve hazzardCurve, IIrCurve discountCurve, double recoveryRate, bool payAccruedOnDefault = true)
        {
            var nodeDates = FixedSchedule.Flows.Select(f => f.AccrualPeriodEnd).ToArray();
            var pv = 0.0;

            //contingent leg
            var d = hazzardCurve.OriginDate;
            foreach(var nd in nodeDates)
            {
                var deltaT = d.CalculateYearFraction(nd, hazzardCurve.Basis);
                var s = hazzardCurve.GetSurvivalProbability(d);
                var dd = discountCurve.GetDf(discountCurve.BuildDate, d);
                var lambda = System.Math.Log(s / hazzardCurve.GetSurvivalProbability(nd)) / deltaT;
                var f = System.Math.Log(dd / discountCurve.GetDf(discountCurve.BuildDate, nd)) / deltaT;
                var term1 = (lambda == 0 && f == 0) ? 1.0 : lambda / (lambda + f);
                pv += term1 * (1.0 - System.Math.Exp(-deltaT * (lambda + f))) * s * dd;
                d = nd;
            }

            pv *= (1.0 - recoveryRate) * Notional;

            //fixed leg
            foreach (var f in FixedSchedule.Flows)
            {
                pv -= f.Notional * f.YearFraction * Spread * discountCurve.GetDf(discountCurve.BuildDate, f.SettleDate) * hazzardCurve.GetSurvivalProbability(f.SettleDate);
            }

            return pv;
        }

        public double PV_LinearApprox(HazzardCurve hazzardCurve, IIrCurve discountCurve, double recoveryRate, bool payAccruedOnDefault = true)
        {
            var nodeDates = DatePeriodType.M.GenerateDateSchedule(hazzardCurve.OriginDate, FinalSensitivityDate);
            var ts = nodeDates.Select(d => discountCurve.BuildDate.CalculateYearFraction(d, DayCountBasis.ACT365F)).ToArray();

            var integrandD = new Func<DateTime,double> (d => discountCurve.GetDf(discountCurve.BuildDate, d) * -hazzardCurve.GetSurvivalProbabilitySlope(d));
            var integrandT = new Func<double, double>(t => integrandD(OriginDate.AddYearFraction(t, DayCountBasis.ACT365F)));

            //contingent leg
            var pv = (1.0 - recoveryRate) * Notional * Integration.SimpsonsRuleExtended(integrandT, 0, ts.Last(), 100);

            //fixed leg
            foreach (var f in FixedSchedule.Flows)
            {
                pv -= f.Notional * f.YearFraction * Spread * discountCurve.GetDf(discountCurve.BuildDate, f.SettleDate) * hazzardCurve.GetSurvivalProbability(f.SettleDate);
            }

            return pv;
        }

        public double PV_SmallSteps(HazzardCurve hazzardCurve, IIrCurve discountCurve, double recoveryRate, bool payAccruedOnDefault = true)
        {
            var nodeDates = DatePeriodType.M.GenerateDateSchedule(hazzardCurve.OriginDate, FinalSensitivityDate);

            //contingent leg
            var pv = 0.0;
            for (var i = 1; i < nodeDates.Length; i++)
            {

                pv += discountCurve.GetDf(discountCurve.BuildDate, nodeDates[i]) 
                    * hazzardCurve.GetDefaultProbability(nodeDates[i - 1], nodeDates[i]);
            }

            pv *= (1.0 - recoveryRate) * Notional;

            //fixed leg
            foreach (var f in FixedSchedule.Flows)
            {
                pv -= f.Notional * f.YearFraction * Spread * discountCurve.GetDf(discountCurve.BuildDate, f.SettleDate) * hazzardCurve.GetSurvivalProbability(f.SettleDate);
            }

            return pv;
        }

        
    }
}
