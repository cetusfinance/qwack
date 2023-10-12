using ProtoBuf;
using Qwack.Transport.BasicTypes;

namespace Qwack.Transport.TransportObjects.Instruments.Funding
{
    [ProtoContract]
    public class TO_FloatRateIndex
    {
        [ProtoMember(1)]
        public DayCountBasis DayCountBasis { get; set; }
        [ProtoMember(2)]
        public DayCountBasis DayCountBasisFixed { get; set; }
        [ProtoMember(3)]
        public string ResetTenor { get; set; }
        [ProtoMember(4)]
        public string ResetTenorFixed { get; set; }
        [ProtoMember(5)]
        public string HolidayCalendars { get; set; }
        [ProtoMember(6)]
        public RollType RollConvention { get; set; }
        [ProtoMember(7)]
        public string Currency { get; set; }
        [ProtoMember(8)]
        public string FixingOffset { get; set; }
        [ProtoMember(9)]
        public string Name { get; set; }
    }
}

