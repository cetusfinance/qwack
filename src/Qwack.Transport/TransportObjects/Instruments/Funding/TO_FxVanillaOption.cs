using System;
using ProtoBuf;
using Qwack.Transport.BasicTypes;
using Qwack.Transport.TransportObjects.Instruments.Asset;

namespace Qwack.Transport.TransportObjects.Instruments.Funding
{
    [ProtoContract]
    public class TO_FxVanillaOption : TO_FxForward
    {
        [ProtoMember(100)]
        public OptionType CallPut { get; set; }
        [ProtoMember(101)]
        public OptionExerciseType ExerciseType { get; set; }
        [ProtoMember(102)]
        public double Premium { get; set; }
        [ProtoMember(103)]
        public DateTime PremiumDate { get; set; }
        [ProtoMember(104)]
        public DateTime ExpiryDate { get; set; }
    }
}
