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
        public TO_ITrsUnderlying Underlying { get; set; }

        public TrsLegType AssetLegResetType { get; set; } = TrsLegType.Bullet;
        public TrsLegType FundingLegResetType { get; set; } = TrsLegType.Resetting;
        public SwapLegType FundingLegType { get; set; } = SwapLegType.Float;
        public FxConversionType FxConversionType { get; set; }


        public TO_FloatRateIndex RateIndex { get; set; }

        public TO_GenericSwapLeg FundingLeg { get; set; }
        public TO_CashFlowSchedule FlowScheduleFunding { get; set; }

        public TO_GenericSwapLeg AssetLeg { get; set; }
        public TO_CashFlowSchedule FlowScheduleAsset { get; set; }

        public string TradeId { get; set; }
        public string Counterparty { get; set; }
        public string PortfolioName { get; set; }

        public double Notional { get; set; }

        public double FundingFixedRateOrMargin { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }


        public string Currency { get; set; }

        public string ForecastFundingCurve { get; set; }

        public string DiscountCurve { get; set; }

        public Dictionary<string, string> MetaData { get; set; }
    }
}
