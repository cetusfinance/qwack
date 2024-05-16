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
        [ProtoMember(118)]
        public TO_IrSwap IrSwap { get; set; }
        [ProtoMember(119)]
        public TO_AssetTrs AssetTrs { get; set; }
        [ProtoMember(120)]
        public TO_AsianBasisSwap AsianBasisSwap { get; set; }
        [ProtoMember(121)]
        public TO_SyntheticCashAndCarry SyntheticCashAndCarry { get; set; }
        [ProtoMember(122)]
        public TO_AssetFxBasisSwap AssetFxBasisSwap { get; set; }
        [ProtoMember(123)]
        public TO_CashBalance CashBalance { get; set; }
        [ProtoMember(124)]
        public TO_UnpricedAverage UnpricedAverage { get; set; }
        [ProtoMember(125)]
        public TO_MultiPeriodBackpricingOption BackpricingOption { get; set; }
        [ProtoMember(126)]
        public TO_AsianLookbackOption AsianLookbackOption { get; set; }
    }
}
