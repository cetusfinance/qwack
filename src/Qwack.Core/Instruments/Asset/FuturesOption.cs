using System;
using System.Collections.Generic;
using Qwack.Core.Basic;
using Qwack.Core.Models;
using Qwack.Transport.BasicTypes;
using Qwack.Transport.TransportObjects.Instruments;
using Qwack.Transport.TransportObjects.Instruments.Asset;

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

        public FuturesOption() { }
        public FuturesOption(TO_FuturesOption to, ICurrencyProvider currencyProvider)
        {
            Currency = currencyProvider.GetCurrencySafe(to.Currency);
            MetaData = to.MetaData;
            Counterparty = to.Counterparty;
            PortfolioName = to.PortfolioName;
            ContractQuantity = to.ContractQuantity;
            LotSize = to.LotSize;
            PriceMultiplier = to.PriceMultiplier;
            Direction = to.Direction;
            ExpiryDate = to.ExpiryDate;
            Strike = to.Strike;
            AssetId = to.AssetId;
            TradeId = to.TradeId;
            PremiumDate = to.PremiumDate;
            Premium = to.Premium;
            CallPut = to.CallPut;
            ExerciseType = to.ExerciseType;
            MarginingType = to.MarginingType;

        }

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

        public override int GetHashCode() => TradeId.GetHashCode() ^ ContractQuantity.GetHashCode() ^ LotSize.GetHashCode() ^ PriceMultiplier.GetHashCode()
            ^ Direction.GetHashCode() ^ ExpiryDate.GetHashCode() ^ Strike.GetHashCode() ^ AssetId.GetHashCode() ^ CallPut.GetHashCode()
            ^ ExerciseType.GetHashCode() ^ MarginingType.GetHashCode() ^ DiscountCurve.GetHashCode() ^ Currency.GetHashCode();

        public new string[] IrCurves(IAssetFxModel model) => string.IsNullOrWhiteSpace(DiscountCurve) ? Array.Empty<string>() : new[] { DiscountCurve };

        public new TO_Instrument ToTransportObject() => new()
        {
            AssetInstrumentType = AssetInstrumentType.FuturesOption,
            FuturesOption = new TO_FuturesOption
            {
                AssetId = AssetId,
                ContractQuantity = ContractQuantity,
                Counterparty = Counterparty,
                Currency = Currency?.Ccy,
                Direction = Direction,
                ExpiryDate = ExpiryDate,
                LotSize = LotSize,
                MetaData = MetaData,
                PortfolioName = PortfolioName,
                PriceMultiplier = PriceMultiplier,
                Strike = Strike,
                TradeId = TradeId,
                CallPut = CallPut,
                ExerciseType = ExerciseType,
                DiscountCurve = DiscountCurve,
                MarginingType = MarginingType,
                Premium = Premium,
                PremiumDate = PremiumDate,
            }
        };
    }
}
