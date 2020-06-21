using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Transport.BasicTypes;

namespace Qwack.Transport.TransportObjects.Instruments.Funding
{
    public class TO_FloatRateIndex
    {
        public DayCountBasis DayCountBasis { get; set; }
        public DayCountBasis DayCountBasisFixed { get; set; }
        public string ResetTenor { get; set; }
        public string ResetTenorFixed { get; set; }

        public string HolidayCalendars { get; set; }
        public RollType RollConvention { get; set; }
        public string Currency { get; set; }
        public string FixingOffset { get; set; }
    }
}

