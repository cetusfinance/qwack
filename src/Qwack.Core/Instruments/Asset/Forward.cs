using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Dates;

namespace Qwack.Core.Instruments.Asset
{
    public class Forward : IAssetInstrument
    {
        public string TradeId { get; set; }
        public double Notional { get; set; }
        public TradeDirection Direction { get; set; }
        public DateTime ExpiryDate { get; set; }
        public Calendar FixingCalendar { get; set; }
        public Calendar PaymentCalendar { get; set; }
        public Frequency SpotLag { get; set; }
        public Frequency PaymentLag { get; set; }
        public DateTime PaymentDate { get; set; }
        public double Strike { get; set; }
        public string AssetId { get; set; }
        public Currency PaymentCurrency { get; set; }
        public string FxFixingId { get; set; }
        public string DiscountCurve { get; set; }

        public AsianSwap AsBulletSwap()
        {
            return new AsianSwap
            {
                TradeId = TradeId,
                Notional = Notional,
                Direction = Direction,
                AverageStartDate = ExpiryDate,
                AverageEndDate = ExpiryDate,
                FixingCalendar = FixingCalendar,
                PaymentCalendar = PaymentCalendar,
                SpotLag = SpotLag,
                PaymentLag = PaymentLag,
                Strike = Strike,
                AssetId = AssetId,
                PaymentCurrency = PaymentCurrency,
                FxFixingId = FxFixingId,
                DiscountCurve = DiscountCurve,
                FixingDates = new[] { ExpiryDate },
                FxFixingDates = new[] { ExpiryDate },
                FxConversionType = FxConversionType.AverageThenConvert,
                PaymentDate = PaymentDate
            };
        }

        public string[] AssetIds => new[] { AssetId };
    }
}
