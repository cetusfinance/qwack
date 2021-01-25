using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Dates;

namespace Qwack.Core.Instruments.Asset
{
    public class EuropeanBarrierOption : EuropeanOption
    {
        public double Barrier { get; set; }
        public BarrierType BarrierType { get; set; }
        public BarrierSide BarrierSide { get; set; }
        public BarrierObservationType BarrierObservationType { get; set; }

        public DateTime BarrierObservationStartDate { get; set; }
        public DateTime BarrierObservationEndDate { get; set; }

        public new IAssetInstrument Clone() => new EuropeanBarrierOption
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
            Counterparty = Counterparty,
            FxConversionType = FxConversionType,
            HedgingSet = HedgingSet,
            PortfolioName = PortfolioName,
            CallPut = CallPut,
            Barrier = Barrier,
            BarrierObservationEndDate = BarrierObservationEndDate,
            BarrierObservationStartDate = BarrierObservationStartDate,
            BarrierObservationType = BarrierObservationType,
            BarrierSide = BarrierSide,
            BarrierType = BarrierType,
        };
        
          
        public new IAssetInstrument SetStrike(double strike)
        {
            var o = (EuropeanBarrierOption)Clone();
            o.Strike = strike;
            return o;
        }

        public override bool Equals(object obj) => obj is EuropeanBarrierOption option &&
                  base.Equals(obj) &&
                  Barrier == option.Barrier &&
                  BarrierType == option.BarrierType &&
                  BarrierObservationType == option.BarrierObservationType &&
                  BarrierObservationStartDate == option.BarrierObservationStartDate &&
                  BarrierObservationEndDate == option.BarrierObservationEndDate &&
                  BarrierSide == option.BarrierSide;

        public override int GetHashCode() => Barrier.GetHashCode() ^ BarrierType.GetHashCode() ^ BarrierObservationType.GetHashCode()
                ^ BarrierObservationStartDate.GetHashCode() ^ BarrierObservationEndDate.GetHashCode() ^ BarrierSide.GetHashCode();
    }
}
