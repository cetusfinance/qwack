using System;

namespace Qwack.Dates
{
    public interface ITenorDate
    {
        DateTime Date(DateTime refDate, RollType rollType, Calendar calendars);
    }
}
