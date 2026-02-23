using ProtoBuf;
using Qwack.Transport.BasicTypes;

namespace Qwack.Transport.TransportObjects.MarketData.Curves
{
    [ProtoContract]
    public class TO_ITimeProvider
    {
        [ProtoMember(1)]
        public TO_CalendarTimeProvider CalendarTimeProvider { get; set; }
        [ProtoMember(2)]
        public TO_BusinessDayTimeProvider BusinessDayTimeProvider { get; set; }

    }

    [ProtoContract]
    public class TO_CalendarTimeProvider
    {
        [ProtoMember(1)]
        public DayCountBasis DayCountBasis { get; set; }
    }

    [ProtoContract]
    public class TO_BusinessDayTimeProvider
    {
        [ProtoMember(1)]
        public string Calendar { get; set; }
        [ProtoMember(2)]
        public double WeekendWeight { get; set; }
        [ProtoMember(3)]
        public double HolidayWeight { get; set; }
    }
}
