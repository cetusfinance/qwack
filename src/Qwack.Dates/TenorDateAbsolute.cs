using System;

namespace Qwack.Dates
{
    /// <summary>
    /// Absolute date implementation of the ITenorDate interface
    /// </summary>
    public class TenorDateAbsolute : ITenorDate
    {
        public TenorDateAbsolute(DateTime absoluteDate) => AbsoluteDate = absoluteDate;

        public DateTime AbsoluteDate { get; set; }

        public DateTime Date(DateTime refDate, RollType rollType, Calendar calendars) => AbsoluteDate;
    }
}
