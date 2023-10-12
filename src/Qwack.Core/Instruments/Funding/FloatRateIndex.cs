using Qwack.Core.Basic;
using Qwack.Dates;
using Qwack.Transport.BasicTypes;
using Qwack.Transport.TransportObjects.Instruments.Funding;

namespace Qwack.Core.Instruments.Funding
{
    public class FloatRateIndex
    {
        public FloatRateIndex() { }
        public FloatRateIndex(TO_FloatRateIndex transportObject, ICalendarProvider calendarProvider, ICurrencyProvider currencyProvider)
        {
            DayCountBasis = transportObject.DayCountBasis;
            DayCountBasisFixed = transportObject.DayCountBasisFixed;
            ResetTenor = new Frequency(transportObject.ResetTenor);
            ResetTenorFixed = new Frequency(transportObject.ResetTenorFixed);
            HolidayCalendars = calendarProvider.GetCalendarSafe(transportObject.HolidayCalendars);
            RollConvention = transportObject.RollConvention;
            Currency = currencyProvider.GetCurrencySafe(transportObject.Currency);
            FixingOffset = new Frequency(transportObject.FixingOffset);
            Name = transportObject.Name;
        }

        public DayCountBasis DayCountBasis { get; set; }
        public DayCountBasis DayCountBasisFixed { get; set; }
        public Frequency ResetTenor { get; set; }
        public Frequency ResetTenorFixed { get; set; }

        public Calendar HolidayCalendars { get; set; }
        public RollType RollConvention { get; set; }
        public Currency Currency { get; set; }
        public Frequency FixingOffset { get; set; }

        public string Name { get; set; }

        public TO_FloatRateIndex GetTransportObject() => new()
        {
            DayCountBasis = DayCountBasis,
            DayCountBasisFixed = DayCountBasisFixed,
            ResetTenor = ResetTenor.ToString(),
            ResetTenorFixed = ResetTenorFixed.ToString(),
            HolidayCalendars = HolidayCalendars.Name,
            RollConvention = RollConvention,
            Currency = Currency.Ccy,
            FixingOffset = FixingOffset.ToString(),
            Name = Name,
        };
    }
}
