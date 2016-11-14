using System.Collections.Generic;
using Qwack.Core.Basic;
using Qwack.Dates;

namespace Qwack.Core.Instruments
{
    public class CashFlowSchedule
    {
        public List<CashFlow> Flows { get; set; }
        public DayCountBasis DayCountBasis { get; set; }
        public ResetType ResetType { get; set; }
        public AverageType AverageType { get; set; }
    }
}
