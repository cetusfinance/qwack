using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;
using Qwack.Transport.BasicTypes;

namespace Qwack.Transport.TransportObjects.MarketData.Correlations
{
    [ProtoContract]
    [ProtoInclude(8,typeof(MultiDimArrayDouble))]
    public class TO_CorrelationMatrix
    {
        [ProtoMember(1)]
        public string[] LabelsX { get; set; }
        [ProtoMember(2)]
        public string[] LabelsY { get; set; }
        [ProtoMember(3)]
        public MultiDimArray<double> Correlations { get; set; }
        [ProtoMember(4)]
        public bool IsTimeVector { get; set; }
        [ProtoMember(5)]
        public MultiDimArray<double> CorrelationsTime { get; set; }
        [ProtoMember(6)]
        public double[] Times { get; set; }
        [ProtoMember(7)]
        public Interpolator1DType InterpolatorType { get; set; }
    }
}
