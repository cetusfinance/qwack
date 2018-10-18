using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Dates;

namespace Qwack.Core.Instruments.Asset
{
    public class FuturesOption : Future, IHasVega
    {
        public OptionType CallPut { get; set; }
        public OptionExerciseType ExerciseType { get; set; }
        public OptionMarginingType MarginingType { get; set; }
        public string DiscountCurve { get; set; }

        public new IAssetInstrument Clone()
        {
            var o = (FuturesOption)base.Clone();
            o.CallPut = CallPut;
            return o;
        }

        public new IAssetInstrument SetStrike(double strike)
        {
            var o = (FuturesOption)base.SetStrike(strike);
            o.CallPut = CallPut;
            return o;
        }

        public override bool Equals(object obj) => obj is FuturesOption futOpt &&
                   TradeId == futOpt.TradeId &&
                   ContractQuantity == futOpt.ContractQuantity &&
                   LotSize == futOpt.LotSize &&
                   PriceMultiplier == futOpt.PriceMultiplier &&
                   Direction == futOpt.Direction &&
                   ExpiryDate == futOpt.ExpiryDate &&
                   Strike == futOpt.Strike &&
                   AssetId == futOpt.AssetId &&
                   CallPut == futOpt.CallPut &&
                   ExerciseType == futOpt.ExerciseType &&
                   MarginingType == futOpt.MarginingType &&
                   DiscountCurve == futOpt.DiscountCurve &&
                   EqualityComparer<Currency>.Default.Equals(Currency, futOpt.Currency);
    }
}
