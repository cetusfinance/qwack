using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;
using Qwack.Transport.BasicTypes;

namespace Qwack.Transport.TransportObjects.Instruments.Funding
{
    [ProtoContract]
    public class TO_CashFlowSchedule
    {
        [ProtoMember(1)]
        public List<TO_Cashflow> Flows { get; set; }
        [ProtoMember(2)]
        public DayCountBasis DayCountBasis { get; set; }
        [ProtoMember(3)]
        public ResetType ResetType { get; set; }
        [ProtoMember(4)]
        public AverageType AverageType { get; set; }
    }
}
