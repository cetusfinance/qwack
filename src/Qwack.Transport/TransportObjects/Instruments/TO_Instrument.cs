using System;
using System.Collections.Generic;
using ProtoBuf;
using Qwack.Transport.BasicTypes;
using Qwack.Transport.TransportObjects.Instruments.Asset;
using Qwack.Transport.TransportObjects.Instruments.Funding;

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
        [ProtoMember(111)]
        public TO_FxForward FxForward { get; set; }
        [ProtoMember(112)]
        public TO_FxFuture FxFuture { get; set; }
        [ProtoMember(113)]
        public TO_FxVanillaOption FxOption { get; set; }
        [ProtoMember(114)]
        public TO_FxPerpetual FxPerpetual { get; set; }
        [ProtoMember(115)]
        public TO_CashWrapper CashWrapper { get; set; }
        [ProtoMember(116)]
        public TO_InflationFwd InflationFwd { get; set; }
        [ProtoMember(117)]
        public TO_InflationPerformanceSwap InflationPerfSwap { get; set; }

        public override bool Equals(object obj) => obj is TO_Instrument instrument
                                                   && FundingInstrumentType == instrument.FundingInstrumentType
                                                   && AssetInstrumentType == instrument.AssetInstrumentType
                                                   && EqualityComparer<TO_AsianSwap>.Default.Equals(AsianSwap, instrument.AsianSwap)
                                                   && EqualityComparer<TO_AsianSwapStrip>.Default.Equals(AsianSwapStrip, instrument.AsianSwapStrip)
                                                   && EqualityComparer<TO_AsianOption>.Default.Equals(AsianOption, instrument.AsianOption)
                                                   && EqualityComparer<TO_Forward>.Default.Equals(Forward, instrument.Forward)
                                                   && EqualityComparer<TO_EuropeanOption>.Default.Equals(EuropeanOption, instrument.EuropeanOption)
                                                   && EqualityComparer<TO_Future>.Default.Equals(Future, instrument.Future)
                                                   && EqualityComparer<TO_FuturesOption>.Default.Equals(FuturesOption, instrument.FuturesOption)
                                                   && EqualityComparer<TO_Equity>.Default.Equals(Equity, instrument.Equity)
                                                   && EqualityComparer<TO_Bond>.Default.Equals(Bond, instrument.Bond)
                                                   && EqualityComparer<TO_FxForward>.Default.Equals(FxForward, instrument.FxForward)
                                                   && EqualityComparer<TO_FxFuture>.Default.Equals(FxFuture, instrument.FxFuture)
                                                   && EqualityComparer<TO_FxVanillaOption>.Default.Equals(FxOption, instrument.FxOption)
                                                   && EqualityComparer<TO_FxPerpetual>.Default.Equals(FxPerpetual, instrument.FxPerpetual)
                                                   && EqualityComparer<TO_CashWrapper>.Default.Equals(CashWrapper, instrument.CashWrapper)
                                                   && EqualityComparer<TO_InflationFwd>.Default.Equals(InflationFwd, instrument.InflationFwd)
                                                   && EqualityComparer<TO_InflationPerformanceSwap>.Default.Equals(InflationPerfSwap, instrument.InflationPerfSwap);

    }
}
