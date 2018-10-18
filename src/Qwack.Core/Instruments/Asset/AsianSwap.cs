using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Core.Models;
using Qwack.Dates;

namespace Qwack.Core.Instruments.Asset
{
    public class AsianSwap : IAssetInstrument
    {
        public string TradeId { get; set; }
        public string Counterparty { get; set; }

        public double Notional { get; set; }
        public TradeDirection Direction { get; set; }

        public DateTime AverageStartDate { get; set; }
        public DateTime AverageEndDate { get; set; }
        public DateTime[] FixingDates { get; set; }
        public Calendar FixingCalendar { get; set; }
        public Calendar PaymentCalendar { get; set; }
        public Frequency SpotLag { get; set; }
        public RollType SpotLagRollType { get; set; } = RollType.F;
        public Frequency PaymentLag { get; set; }
        public RollType PaymentLagRollType { get; set; } = RollType.F;
        public DateTime PaymentDate { get; set; }
        public double Strike { get; set; }
        public string AssetId { get; set; }
        public string AssetFixingId { get; set; }
        public string FxFixingId { get; set; }
        public DateTime[] FxFixingDates { get; set; }
        public Currency PaymentCurrency { get; set; }
        public FxConversionType FxConversionType { get; set; } = FxConversionType.None;
        public string DiscountCurve { get; set; }

        public string[] AssetIds => new[] { AssetId };
        public string[] IrCurves => new[] { DiscountCurve };
        public Currency Currency => PaymentCurrency;

        public DateTime LastSensitivityDate => PaymentDate.Max(AverageEndDate.AddPeriod(SpotLagRollType, FixingCalendar, SpotLag));

        public IAssetInstrument Clone()
        {
            return new AsianSwap
            {
                TradeId = TradeId,
                Notional = Notional,
                Direction = Direction,
                AverageStartDate = AverageStartDate,
                AverageEndDate = AverageEndDate,
                FixingDates = (DateTime[])FixingDates.Clone(),
                FixingCalendar = FixingCalendar,
                PaymentCalendar = PaymentCalendar,
                SpotLag = SpotLag,
                SpotLagRollType = SpotLagRollType,
                PaymentLag = PaymentLag,
                PaymentLagRollType = PaymentLagRollType,
                PaymentDate = PaymentDate,
                PaymentCurrency = PaymentCurrency,
                AssetFixingId = AssetFixingId,
                AssetId = AssetId,
                DiscountCurve = DiscountCurve,
                FxConversionType = FxConversionType,
                FxFixingDates = FxFixingDates == null ? null : (DateTime[])FxFixingDates.Clone(),
                FxFixingId = FxFixingId,
                Strike = Strike
            };
        }

        public IAssetInstrument SetStrike(double strike)
        {
            var c = (AsianSwap)Clone();
            c.Strike = strike;
            return c;
        }

        public FxConversionType FxType(IAssetFxModel model) => model.GetPriceCurve(AssetId).Currency == PaymentCurrency ? FxConversionType.None : FxConversionType;

        public string FxPair(IAssetFxModel model) => model.GetPriceCurve(AssetId).Currency == PaymentCurrency ? string.Empty : $"{model.GetPriceCurve(AssetId).Currency}/{PaymentCurrency}";

        public Dictionary<string, List<DateTime>> PastFixingDates(DateTime valDate) => valDate <= FixingDates.First() ?
                new Dictionary<string, List<DateTime>>() :
                new Dictionary<string, List<DateTime>> { { AssetId, FixingDates.Where(d => d < valDate).ToList() } };
    }
}
