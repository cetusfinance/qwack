using System;
using System.Collections.Generic;
using Qwack.Core.Basic;
using Qwack.Core.Models;
using Qwack.Transport.BasicTypes;
using Qwack.Transport.TransportObjects.Instruments;
using Qwack.Transport.TransportObjects.Instruments.Asset;

namespace Qwack.Core.Instruments.Asset
{
    public class Future : IAssetInstrument
    {
        public Dictionary<string, string> MetaData { get; set; } = new Dictionary<string, string>();
        public string TradeId { get; set; }
        public string Counterparty { get; set; }
        public string PortfolioName { get; set; }
        public double ContractQuantity { get; set; }
        public double LotSize { get; set; }
        public double PriceMultiplier { get; set; } = 1.0;
        public TradeDirection Direction { get; set; }

        public DateTime ExpiryDate { get; set; }

        public double Strike { get; set; }
        public string AssetId { get; set; }

        public Currency Currency { get; set; }
        public Currency PaymentCurrency => Currency;

        public string[] AssetIds => new[] { AssetId };

        public Future() { }
        public Future(TO_Future to, ICurrencyProvider currencyProvider)
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
        }

        public IAssetInstrument Clone() => new Future
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
            MetaData = MetaData,
        };

        public IAssetInstrument SetStrike(double strike)
        {
            var c = (Future)Clone();
            c.Strike = strike;
            return c;
        }

        public FxConversionType FxType(IAssetFxModel model) => model.GetPriceCurve(AssetId).Currency == Currency ? FxConversionType.None : FxConversionType.ConvertThenAverage;
        public string FxPair(IAssetFxModel model) => model.GetPriceCurve(AssetId).Currency == Currency ? string.Empty : $"{model.GetPriceCurve(AssetId).Currency}/{Currency}";

        public Dictionary<string, List<DateTime>> PastFixingDates(DateTime valDate) => [];
        public Dictionary<string, List<DateTime>> PastFixingDatesFx(IAssetFxModel model, DateTime valDate) => [];
        public DateTime LastSensitivityDate => ExpiryDate;

        public string[] IrCurves(IAssetFxModel model) => Array.Empty<string>();

        public override bool Equals(object obj) => obj is Future future &&
                   TradeId == future.TradeId &&
                   ContractQuantity == future.ContractQuantity &&
                   LotSize == future.LotSize &&
                   PriceMultiplier == future.PriceMultiplier &&
                   Direction == future.Direction &&
                   ExpiryDate == future.ExpiryDate &&
                   Strike == future.Strike &&
                   AssetId == future.AssetId &&
                   EqualityComparer<Currency>.Default.Equals(Currency, future.Currency);

        public override int GetHashCode() => TradeId.GetHashCode() ^ ContractQuantity.GetHashCode() ^ LotSize.GetHashCode()
            ^ PriceMultiplier.GetHashCode() ^ Direction.GetHashCode() ^ ExpiryDate.GetHashCode() ^ Strike.GetHashCode()
            ^ AssetId.GetHashCode() ^ Currency.GetHashCode();

        public TO_Instrument ToTransportObject() => new()
        {
            AssetInstrumentType = AssetInstrumentType.Future,
            Future = new TO_Future
            {
                AssetId = AssetId,
                ContractQuantity = ContractQuantity,
                Counterparty = Counterparty,
                Currency = Currency?.Ccy,
                Direction = Direction,
                ExpiryDate = ExpiryDate,
                LotSize = LotSize,
                PortfolioName = PortfolioName,
                PriceMultiplier = PriceMultiplier,
                Strike = Strike,
                TradeId = TradeId,
                MetaData = new(MetaData),
            }
        };
    }
}
