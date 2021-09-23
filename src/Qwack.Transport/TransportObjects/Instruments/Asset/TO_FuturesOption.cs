using System;
using ProtoBuf;
using Qwack.Transport.BasicTypes;

namespace Qwack.Transport.TransportObjects.Instruments.Asset
{
    [ProtoContract]
    public class TO_FuturesOption : TO_Future
    {
        [ProtoMember(100)]
        public OptionType CallPut { get; set; }
        [ProtoMember(101)]
        public OptionExerciseType ExerciseType { get; set; }
        [ProtoMember(102)]
        public OptionMarginingType MarginingType { get; set; }
        [ProtoMember(103)]
        public string DiscountCurve { get; set; }
        [ProtoMember(104)]
        public double Premium { get; set; }
        [ProtoMember(105)]
        public DateTime PremiumDate { get; set; }
    }
}
