using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Qwack.Transport.BasicTypes;

namespace Qwack.Transport.TransportObjects.Instruments.Funding
{
    public class TO_CashFlowSchedule
    {

        public List<TO_Cashflow> Flows { get; set; }
        public DayCountBasis DayCountBasis { get; set; }
        public ResetType ResetType { get; set; }
        public AverageType AverageType { get; set; }
    }
}
