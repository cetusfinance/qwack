using System.Collections.Generic;

namespace Qwack.Dates
{
    public class FutureDatesGenerator
    {
        private Calendar _calendarObject;
        private string _calendar;

        public string Calendar
        {
            get => _calendar;
            set
            {
                _calendar = value;
                if (!_calendarProvider.Collection.TryGetCalendar(_calendar, out _calendarObject)) throw new KeyNotFoundException($"Could not find the calendar {_calendar}");
            }
        }

        public Calendar CalendarObject => _calendarObject;
        public int MonthModifier { get; set; }
        public int DayOfMonthToStart { get; set; }
        public string DayOfMonthToStartOther { get; set; }
        public string DateOffsetModifier { get; set; }
        public bool DoMToStartIsNumber { get; set; }
        public bool NeverExpires { get; set; }
        public string FixedFuture { get; set; }

        private readonly ICalendarProvider _calendarProvider;

        public FutureDatesGenerator(ICalendarProvider calendarProvider) => _calendarProvider = calendarProvider;
    }
}
