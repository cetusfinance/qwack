using System;

namespace Qwack.Dates
{
    public class TenorDateAbsolute : ITenorDate
    {
        public TenorDateAbsolute(DateTime absoluteDate)
        {
            AbsoluteDate = absoluteDate;
        }

        public DateTime AbsoluteDate { get; set; }
                
        public DateTime Date(DateTime refDate, RollType rollType, Calendar calendars)
        {
            return AbsoluteDate;
        }
    }
}
