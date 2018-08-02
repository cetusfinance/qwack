using System;
using System.Collections.Generic;
using System.Text;

namespace Qwack.Dates
{
    public class MarketOpenOverride
    {
        public DayOfWeek DoW { get; set; }
        public TimeSpan OpenTime { get; set; }
        public int DayModifier { get; set; }

        public MarketOpenOverride()
        { }

        public MarketOpenOverride(DayOfWeek DayOfWeek, TimeSpan OTime, int Dmodifier)
        {
            DoW = DayOfWeek;
            OpenTime = OTime;
            DayModifier = Dmodifier;
        }
    }
}
