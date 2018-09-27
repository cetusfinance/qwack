using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Core.Models;
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
        public FxConversionType FxConversionType { get; set; } = FxConversionType.None;

        public string[] IrCurves => new[] { DiscountCurve };

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

        public IAssetInstrument Clone() => new Forward
        {
            TradeId = TradeId,
            Notional = Notional,
            Direction = Direction,
            ExpiryDate = ExpiryDate,
            FixingCalendar = FixingCalendar,
            PaymentCalendar = PaymentCalendar,
            SpotLag = SpotLag,
            PaymentLag = PaymentLag,
            Strike = Strike,
            AssetId = AssetId,
            PaymentCurrency = PaymentCurrency,
            FxFixingId = FxFixingId,
            DiscountCurve = DiscountCurve,
            PaymentDate = PaymentDate,
        };

        public IAssetInstrument SetStrike(double strike)
        {
            var o = (Forward)Clone();
            o.Strike = strike;
            return o;
        }

        public string[] AssetIds => new[] { AssetId };

        public DateTime LastSensitivityDate => PaymentDate.Max(ExpiryDate);

        public FxConversionType FxType(IAssetFxModel model) => model.GetPriceCurve(AssetId).Currency == PaymentCurrency ? FxConversionType.None : FxConversionType;
        public string FxPair(IAssetFxModel model) => model.GetPriceCurve(AssetId).Currency == PaymentCurrency ? string.Empty : $"{model.GetPriceCurve(AssetId).Currency}/{PaymentCurrency}";

        public Dictionary<string, List<DateTime>> PastFixingDates(DateTime valDate) => valDate <= ExpiryDate ?
            new Dictionary<string, List<DateTime>>() :
            new Dictionary<string, List<DateTime>> { { AssetId, new List<DateTime> { ExpiryDate } } };
    }
}
