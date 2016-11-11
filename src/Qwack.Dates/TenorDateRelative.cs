using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Qwack.Dates
{
    public class TenorDateRelative : ITenorDate
    {
        public TenorDateRelative(Frequency relativeTenor)
        {
            RelativeTenor = relativeTenor;
        }

        public Frequency RelativeTenor { get; set; }

        public DateTime Date(DateTime refDate, RollType rollType, Calendar calendars)
        {
            return refDate.AddPeriod(rollType, calendars, RelativeTenor);
        }
    }
}
