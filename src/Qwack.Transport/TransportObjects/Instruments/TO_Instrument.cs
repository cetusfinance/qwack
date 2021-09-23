using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;
using Qwack.Transport.BasicTypes;
using Qwack.Transport.TransportObjects.Instruments.Asset;

namespace Qwack.Transport.TransportObjects.Instruments
{
    [ProtoContract]
    public class TO_Instrument
    {
        [ProtoMember(100)]
        public FundingInstrumentType FundingInstrumentType { get; set; } 
        [ProtoMember(101)]
        public AssetInstrumentType AssetInstrumentType { get; set; }
        [ProtoMember(102)]
        public TO_AsianSwap AsianSwap { get; set; }
        [ProtoMember(103)]
        public TO_AsianSwapStrip AsianSwapStrip { get; set; }
        [ProtoMember(104)]
        public TO_AsianOption AsianOption { get; set; }
        [ProtoMember(105)]
        public TO_Forward Forward { get; set; }
        [ProtoMember(106)]
        public TO_EuropeanOption EuropeanOption { get; set; }
        [ProtoMember(107)]
        public TO_Future Future { get; set; }
        [ProtoMember(108)]
        public TO_FuturesOption FuturesOption { get; set; }
        [ProtoMember(109)]
        public TO_Equity Equity { get; set; }
        [ProtoMember(110)]
        public TO_Bond Bond { get; set; }

        public override bool Equals(object obj) => obj is TO_Instrument instrument && 
            FundingInstrumentType == instrument.FundingInstrumentType && 
            AssetInstrumentType == instrument.AssetInstrumentType && 
            EqualityComparer<TO_AsianSwap>.Default.Equals(AsianSwap, instrument.AsianSwap) && 
            EqualityComparer<TO_AsianSwapStrip>.Default.Equals(AsianSwapStrip, instrument.AsianSwapStrip) && 
            EqualityComparer<TO_AsianOption>.Default.Equals(AsianOption, instrument.AsianOption) && 
            EqualityComparer<TO_Forward>.Default.Equals(Forward, instrument.Forward) && 
            EqualityComparer<TO_EuropeanOption>.Default.Equals(EuropeanOption, instrument.EuropeanOption) && 
            EqualityComparer<TO_Future>.Default.Equals(Future, instrument.Future) && 
            EqualityComparer<TO_FuturesOption>.Default.Equals(FuturesOption, instrument.FuturesOption) && 
            EqualityComparer<TO_Equity>.Default.Equals(Equity, instrument.Equity) && 
            EqualityComparer<TO_Bond>.Default.Equals(Bond, instrument.Bond);

        public override int GetHashCode()
        {
            var hashCode = 243093849;
            hashCode = hashCode * -1521134295 + FundingInstrumentType.GetHashCode();
            hashCode = hashCode * -1521134295 + AssetInstrumentType.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<TO_AsianSwap>.Default.GetHashCode(AsianSwap);
            hashCode = hashCode * -1521134295 + EqualityComparer<TO_AsianSwapStrip>.Default.GetHashCode(AsianSwapStrip);
            hashCode = hashCode * -1521134295 + EqualityComparer<TO_AsianOption>.Default.GetHashCode(AsianOption);
            hashCode = hashCode * -1521134295 + EqualityComparer<TO_Forward>.Default.GetHashCode(Forward);
            hashCode = hashCode * -1521134295 + EqualityComparer<TO_EuropeanOption>.Default.GetHashCode(EuropeanOption);
            hashCode = hashCode * -1521134295 + EqualityComparer<TO_Future>.Default.GetHashCode(Future);
            hashCode = hashCode * -1521134295 + EqualityComparer<TO_FuturesOption>.Default.GetHashCode(FuturesOption);
            hashCode = hashCode * -1521134295 + EqualityComparer<TO_Equity>.Default.GetHashCode(Equity);
            hashCode = hashCode * -1521134295 + EqualityComparer<TO_Bond>.Default.GetHashCode(Bond);
            return hashCode;
        }
    }
}
