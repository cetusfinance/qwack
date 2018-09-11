using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
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
    }
}
