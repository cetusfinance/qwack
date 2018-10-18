using Qwack.Core.Basic;
using Qwack.Dates;

namespace Qwack.Core.Instruments.Funding
{
    public class FloatRateIndex
    {
        public DayCountBasis DayCountBasis { get; set; }
        public DayCountBasis DayCountBasisFixed { get; set; }
        public Frequency ResetTenor { get; set; }
        public Frequency ResetTenorFixed { get; set; }

        public Calendar HolidayCalendars { get; set; }
        public RollType RollConvention { get; set; }
        public Currency Currency { get; set; }
        public Frequency FixingOffset { get; set; }
    }
}
