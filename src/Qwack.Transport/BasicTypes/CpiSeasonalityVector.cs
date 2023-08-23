using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;

namespace Qwack.Transport.BasicTypes
{
    [ProtoContract]
    public class CpiSeasonalityVector
    {
        [ProtoMember(1)]
        public double[] SeasonalityFactors { get; set; }
        [ProtoMember(2)]
        public string CurveName { get; set; }
    }
}
