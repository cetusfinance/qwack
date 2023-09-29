using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;
using Qwack.Transport.BasicTypes;
using Qwack.Transport.TransportObjects.Instruments.Funding;

namespace Qwack.Transport.TransportObjects.Instruments.Asset
{
    [ProtoContract]
    public class TO_AssetTrs
    {
        [ProtoMember(1)]
        public TO_ITrsUnderlying Underlying { get; set; }

        [ProtoMember(2)]
        public TrsLegType AssetLegResetType { get; set; } = TrsLegType.Bullet;
        [ProtoMember(3)]
        public TrsLegType FundingLegResetType { get; set; } = TrsLegType.Resetting;
        [ProtoMember(4)]
        public SwapLegType FundingLegType { get; set; } = SwapLegType.Float;
        [ProtoMember(5)]
        public FxConversionType FxConversionType { get; set; }

        [ProtoMember(6)]
        public string AssetLegPaymentOffset { get; set; }
        [ProtoMember(7)]
        public string FundingLegPaymentOffset { get; set; }
        [ProtoMember(8)]
        public string SettleCalendar { get; set; }

        [ProtoMember(9)]
        public TO_FloatRateIndex RateIndex { get; set; }

        [ProtoMember(10)]
        public TO_GenericSwapLeg FundingLeg { get; set; }
        [ProtoMember(11)]
        public TO_CashFlowSchedule FlowScheduleFunding { get; set; }

        [ProtoMember(12)]
        public TO_GenericSwapLeg AssetLeg { get; set; }
        [ProtoMember(13)]
        public TO_CashFlowSchedule FlowScheduleAsset { get; set; }

        [ProtoMember(14)]
        public string TradeId { get; set; }
        [ProtoMember(15)]
        public string Counterparty { get; set; }
        [ProtoMember(16)]
        public string PortfolioName { get; set; }
        
        [ProtoMember(17)]
        public double Notional { get; set; }

        [ProtoMember(18)]
        public double FundingFixedRateOrMargin { get; set; }

        [ProtoMember(19)]
        public DateTime StartDate { get; set; }
        [ProtoMember(20)]
        public DateTime EndDate { get; set; }

        [ProtoMember(21)]
        public string Currency { get; set; }

        [ProtoMember(22)]
        public string ForecastFundingCurve { get; set; }

        [ProtoMember(23)]
        public string DiscountCurve { get; set; }

        [ProtoMember(24)]
        public Dictionary<string, string> MetaData { get; set; }

        [ProtoMember(25)]
        public double? InitialAssetFixing { get; set; }
    }
}
