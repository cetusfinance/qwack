using Qwack.Transport.BasicTypes;

namespace Qwack.Transport.TransportObjects.Instruments.Funding
{
    public class TO_InflationIndex
    {
        public DayCountBasis DayCountBasis { get; set; }
        public DayCountBasis DayCountBasisFixed { get; set; }
        public string FixingLag { get; set; }
        public string ResetFrequency { get; set; }

        public string HolidayCalendars { get; set; }
        public RollType RollConvention { get; set; }
        public string Currency { get; set; }
        public Interpolator1DType FixingInterpolation { get; set; }
    }
}

