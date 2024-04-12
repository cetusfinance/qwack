using System;
using ProtoBuf;

namespace Qwack.Transport.BasicTypes
{
    [ProtoContract]
    public class DateArray
    {
        public DateArray() { }
        public DateArray(DateTime[] data) { Dates = data; }

        [ProtoMember(1)]
        public DateTime[] Dates { get; set; }
    }
}
