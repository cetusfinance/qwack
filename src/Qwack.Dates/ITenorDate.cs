using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Qwack.Dates
{
    public interface ITenorDate
    {
        DateTime Date(DateTime refDate, RollType rollType, Calendar calendars);
    }
}
