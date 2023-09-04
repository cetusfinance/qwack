using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Instruments.Funding;
using Qwack.Core.Models;
using Qwack.Transport.BasicTypes;

namespace Qwack.Core.Instruments.Asset
{
    public class AssetTRS : IAssetInstrument
    {
        public AssetTRS() { }

        public ITrsUnderlying Underlying { get; set; }

        public string[] AssetIds => Underlying.AssetIds;

        public Currency PaymentCurrency => Currency;

        public GenericSwapLeg FundingLeg { get; set; }
        public CashFlowSchedule FlowScheduleFunding { get; set; }

        public string TradeId { get; set; }        
        public string Counterparty { get; set; }
        public string PortfolioName { get; set; }

        public double Notional { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public DateTime LastSensitivityDate => EndDate;

        public Currency Currency { get; set; }

        public Dictionary<string, string> MetaData { get; set; }

        public IAssetInstrument Clone() => throw new NotImplementedException();
        public string FxPair(IAssetFxModel model) => throw new NotImplementedException();
        public FxConversionType FxType(IAssetFxModel model) => throw new NotImplementedException();
        public string[] IrCurves(IAssetFxModel model) => throw new NotImplementedException();
        public Dictionary<string, List<DateTime>> PastFixingDates(DateTime valDate) => throw new NotImplementedException();
        public IAssetInstrument SetStrike(double strike) => throw new NotImplementedException();
    }
}
