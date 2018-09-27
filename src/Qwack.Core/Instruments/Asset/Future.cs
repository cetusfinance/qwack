using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Core.Models;
using Qwack.Dates;

namespace Qwack.Core.Instruments.Asset
{
    public class Future : IAssetInstrument
    {
        public string TradeId { get; set; }

        public double ContractQuantity { get; set; }
        public double LotSize { get; set; }
        public double PriceMultiplier { get; set; } = 1.0;
        public TradeDirection Direction { get; set; }

        public DateTime ExpiryDate { get; set; }

        public double Strike { get; set; }
        public string AssetId { get; set; }

        public Currency Currency { get; set; }

        public string[] AssetIds => new[] { AssetId };

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
            TradeId = TradeId
        };

        public IAssetInstrument SetStrike(double strike)
        {
            var c = (Future)Clone();
            c.Strike = strike;
            return c;
        }

        public FxConversionType FxType(IAssetFxModel model) => model.GetPriceCurve(AssetId).Currency == Currency ? FxConversionType.None : FxConversionType.ConvertThenAverage;
        public string FxPair(IAssetFxModel model) => model.GetPriceCurve(AssetId).Currency == Currency ? string.Empty : $"{model.GetPriceCurve(AssetId).Currency}/{Currency}";

        public Dictionary<string, List<DateTime>> PastFixingDates(DateTime valDate) => new Dictionary<string, List<DateTime>>();

        public DateTime LastSensitivityDate => ExpiryDate;

        public string[] IrCurves => new string[0];
    }
}
