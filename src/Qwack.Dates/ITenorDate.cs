using System;

namespace Qwack.Dates
{
    /// <summary>
    /// Provides a type to be used when a fixed/absolute or relative date might need to be used
    /// </summary>
    public interface ITenorDate
    {
        DateTime Date(DateTime refDate, RollType rollType, Calendar calendars);
    }
}
