using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Core.Instruments.Funding;
using Qwack.Dates;

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

        public GenericSwapLeg FixedLeg { get; set; }
        public CashFlowSchedule FixedSchedule { get; set; }
        public CDS()
        {
            FixedLeg = new GenericSwapLeg(OriginDate, Tenor, HolidayCalendar, Currency, new Frequency("1m"), Basis);
            FixedLeg.RollDay = "IMM";
            FixedSchedule = FixedLeg.GenerateSchedule();
            
        }

        //http://www.bnikolic.co.uk/cds/cdsvaluation.html 
        public double PV(HazzardCurve hazzardCurve, ICurve discountCurve, double recoveryRate, bool payAccruedOnDefault=true)
        {
            //var contingentLeg = (1.0 - recoveryRate) *
            return 0;
        }
    }
}
