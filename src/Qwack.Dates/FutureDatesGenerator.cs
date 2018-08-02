using System;
using System.Collections.Generic;
using System.Text;

namespace Qwack.Dates
{
    public class FutureDatesGenerator
    {
        private bool _DoMToStartIsNumber;

        public string Calendar { get; set; }
        public int MonthModifier { get; set; }
        public int DayOfMonthToStart { get; set; }
        public string DateOffsetModifier { get; set; }
        public bool DoMToStartIsNumber { get; set; }
        public bool NeverExpires { get; set; }
        public string FixedFuture { get; set; }


    }
}
