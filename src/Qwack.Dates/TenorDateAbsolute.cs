using System;
using Qwack.Transport.BasicTypes;

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
        public override bool Equals(object obj) => obj is TenorDateAbsolute absolute && AbsoluteDate == absolute.AbsoluteDate;
    }
}
