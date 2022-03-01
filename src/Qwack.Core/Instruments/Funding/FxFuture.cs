using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Basic.Capital;
using Qwack.Core.Instruments.Asset;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Transport.BasicTypes;

namespace Qwack.Core.Instruments.Funding
{
    public class FxFuture : FxForward, IHasVega, IAssetInstrument
    {
        public FxFuture() { }

        public override double SupervisoryDelta(IAssetFxModel model) => 1.0;
        public override double EffectiveNotional(IAssetFxModel model, double? MPOR = null) => SupervisoryDelta(model) * AdjustedNotional(model) * MaturityFactor(model.BuildDate, MPOR);
   
        public override IAssetInstrument Clone() => new FxFuture()
        {
            Counterparty = Counterparty,
            DeliveryDate = DeliveryDate,
            DomesticCCY = DomesticCCY,
            DomesticQuantity = DomesticQuantity,
            ForeignCCY = ForeignCCY,
            Strike = Strike,
            TradeId = TradeId
        };

        public override double Pv(IFundingModel Model, bool updateState, bool ignoreTodayFlows)
        {
            if (Model.BuildDate > DeliveryDate || (ignoreTodayFlows && Model.BuildDate == DeliveryDate))
                return 0.0;

            var fwdRate = Model.GetFxRate(DeliveryDate, DomesticCCY, ForeignCCY);
            var FV = (fwdRate - Strike) * DomesticQuantity;

            return FV;
        }

        public override List<CashFlow> ExpectedCashFlows(IAssetFxModel model) => new List<CashFlow>
            {
                new CashFlow()
                {
                    Currency = DomesticCCY,
                    SettleDate = DeliveryDate,
                    Notional = DomesticQuantity,
                    Fv = DomesticQuantity
                },
                new CashFlow()
                {
                    Currency = ForeignCCY,
                    SettleDate = DeliveryDate,
                    Notional = DomesticQuantity * Strike,
                    Fv = DomesticQuantity * Strike
                }
            };
            
    }
}
