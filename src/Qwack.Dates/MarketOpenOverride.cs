using System;
using System.Collections.Generic;
using System.Text;

namespace Qwack.Dates
{
    public class MarketOpenOverride
    {
        public DayOfWeek DayOfWeek { get; set; }
        public TimeSpan OpenTime { get; set; }
        public int DayModifier { get; set; }

        public MarketOpenOverride()
        { }

        public MarketOpenOverride(DayOfWeek dayOfWeek, TimeSpan openTime, int dayModifier)
        {
            DayOfWeek = dayOfWeek;
            OpenTime = openTime;
            DayModifier = dayModifier;
        }
    }
}
