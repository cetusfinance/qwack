using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Qwack.Dates
{
    public class TenorDateAbsolute : ITenorDate
    {
        public TenorDateAbsolute(DateTime absoluteDate)
        {
            AbsoluteDate = absoluteDate;
        }
        public DateTime Date(DateTime refDate, RollType rollType, string calendars)
        {
            return AbsoluteDate;
        }

        public DateTime AbsoluteDate { get; set; }

        public DateTime Date(DateTime refDate, RollType rollType, Calendar calendars)
        {
            return AbsoluteDate;
        }
    }
}
