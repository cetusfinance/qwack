using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Core.Models;
using Qwack.Dates;

namespace Qwack.Core.Instruments.Asset
{
    public class FuturesOption : Future, IHasVega
    {
        public OptionType CallPut { get; set; }
        public OptionExerciseType ExerciseType { get; set; }
        public OptionMarginingType MarginingType { get; set; }
        public string DiscountCurve { get; set; }
        public double Premium { get; set; }
        public DateTime PremiumDate { get; set; }

        public new IAssetInstrument Clone() => new FuturesOption
        {
            AssetId = AssetId,
            ContractQuantity = ContractQuantity,
            Currency = Currency,
            Direction = Direction,
            ExpiryDate = ExpiryDate,
            LotSize = LotSize,
            PriceMultiplier = PriceMultiplier,
            Strike = Strike,
            TradeId = TradeId,
            Counterparty = Counterparty,
            PortfolioName = PortfolioName,
            CallPut = CallPut,
            DiscountCurve = DiscountCurve,
            ExerciseType = ExerciseType,
            MarginingType = MarginingType,
            Premium = Premium,
            PremiumDate = PremiumDate
        };

        public new IAssetInstrument SetStrike(double strike)
        {
            var o = (FuturesOption)Clone();
            o.Strike = strike;
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

        public new string[] IrCurves(IAssetFxModel model) => string.IsNullOrWhiteSpace(DiscountCurve) ? Array.Empty<string>() : new[] { DiscountCurve };
    }
}
