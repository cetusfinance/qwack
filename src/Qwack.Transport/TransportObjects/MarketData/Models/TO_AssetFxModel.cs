using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;
using Qwack.Transport.TransportObjects.Instruments;
using Qwack.Transport.TransportObjects.MarketData.Correlations;
using Qwack.Transport.TransportObjects.MarketData.Curves;
using Qwack.Transport.TransportObjects.MarketData.VolSurfaces;

namespace Qwack.Transport.TransportObjects.MarketData.Models
{
    [ProtoContract]
    [ProtoInclude(1,typeof(TO_VolSurfaceKey))]
    [ProtoInclude(2, typeof(TO_VolSurface))]
    [ProtoInclude(3, typeof(TO_PriceCurve))]
    [ProtoInclude(4, typeof(TO_FixingDictionary))]
    [ProtoInclude(5, typeof(TO_FundingModel))]
    [ProtoInclude(6, typeof(TO_CorrelationMatrix))]
    [ProtoInclude(7, typeof(TO_Portfolio))]
    public class TO_AssetFxModel
    {
        [ProtoMember(8)]
        public Dictionary<TO_VolSurfaceKey, TO_VolSurface> AssetVols{ get; set; }
        [ProtoMember(9)]
        public Dictionary<string, TO_PriceCurve> AssetCurves { get; set; }
        [ProtoMember(10)]
        public Dictionary<string, TO_FixingDictionary> Fixings { get; set; }
        [ProtoMember(11)]
        public DateTime BuildDate { get; set; }
        [ProtoMember(12)]
        public TO_FundingModel FundingModel { get; set; }
        [ProtoMember(13)]
        public TO_CorrelationMatrix CorrelationMatrix { get; set; }
        [ProtoMember(14)]
        public TO_Portfolio Portfolio { get; set; }

    }
}
