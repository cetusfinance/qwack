using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;

namespace Qwack.Transport.TransportObjects.MarketData.Curves
{
    [ProtoContract]
    public class TO_SeasonalCpiCurve : TO_CPICurve
    {
        [ProtoMember(101)]
        public double[] SeasonalAdjustments { get; set; }
    }
}
