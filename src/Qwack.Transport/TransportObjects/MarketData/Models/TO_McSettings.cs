using System.Collections.Generic;
using ProtoBuf;
using Qwack.Transport.BasicTypes;

namespace Qwack.Transport.TransportObjects.MarketData.Models
{
    [ProtoContract]
    public class TO_McSettings
    {
        [ProtoMember(1)]
        public int NumberOfPaths { get; set; }
        [ProtoMember(2)]
        public int NumberOfTimesteps { get; set; }
        [ProtoMember(3)]
        public RandomGeneratorType Generator { get; set; }
        [ProtoMember(4)]
        public string ReportingCurrency { get; set; }
        [ProtoMember(5)]
        public bool ExpensiveFuturesSimulation { get; set; }
        [ProtoMember(6)]
        public Dictionary<string, string> FuturesMappingTable { get; set; } = new Dictionary<string, string>();
        [ProtoMember(7)]
        public McModelType McModelType { get; set; }
        [ProtoMember(8)]
        public bool LocalCorrelation { get; set; }
        [ProtoMember(9)]
        public bool Parallelize { get; set; }
        [ProtoMember(10)]
        public bool DebugMode { get; set; }
        [ProtoMember(11)]
        public bool AveragePathCorrection { get; set; }
        [ProtoMember(12)]
        public bool CompactMemoryMode { get; set; }
        [ProtoMember(13)]
        public bool AvoidRegressionForBackPricing { get; set; }
        [ProtoMember(14)]
        public TO_CreditSettings CreditSettings { get; set; }
    }
}
