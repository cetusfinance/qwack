using Qwack.Core.Basic;
using Qwack.Dates;
using Qwack.Transport.BasicTypes;
using Qwack.Transport.TransportObjects.Instruments.Funding;

namespace Qwack.Core.Instruments.Funding
{
    public class InflationIndex
    {
        public InflationIndex() { }
        public InflationIndex(TO_InflationIndex transportObject, ICalendarProvider calendarProvider, ICurrencyProvider currencyProvider)
        {
            DayCountBasis = transportObject.DayCountBasis;
            DayCountBasisFixed = transportObject.DayCountBasisFixed;
            FixingLag = new Frequency(transportObject.FixingLag);
            ResetFrequency = new Frequency(transportObject.ResetFrequency);
            HolidayCalendars = calendarProvider.GetCalendarSafe(transportObject.HolidayCalendars);
            RollConvention = transportObject.RollConvention;
            Currency = currencyProvider.GetCurrencySafe(transportObject.Currency);
            FixingInterpolation = transportObject.FixingInterpolation;
            Name = transportObject.Name;
        }

        public string Name { get; set; }
        public DayCountBasis DayCountBasis { get; set; }
        public DayCountBasis DayCountBasisFixed { get; set; }
        public Frequency FixingLag { get; set; }
        public Frequency ResetFrequency { get; set; }

        public Calendar HolidayCalendars { get; set; }
        public RollType RollConvention { get; set; }
        public Currency Currency { get; set; }
        public Interpolator1DType FixingInterpolation { get; set; }

        public TO_InflationIndex GetTransportObject() => new()
        {
            DayCountBasis = DayCountBasis,
            DayCountBasisFixed = DayCountBasisFixed,
            FixingInterpolation = FixingInterpolation,
            Currency = Currency.Ccy,
            FixingLag = FixingLag.ToString(),
            HolidayCalendars = HolidayCalendars.Name,
            ResetFrequency = ResetFrequency.ToString(),
            RollConvention = RollConvention,
            Name = Name,
        };   
    }
}
