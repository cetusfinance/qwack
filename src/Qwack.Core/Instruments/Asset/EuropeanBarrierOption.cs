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

        public new IAssetInstrument Clone()
        {
            var o = (EuropeanBarrierOption)base.Clone();
            o.Barrier = Barrier;
            o.BarrierObservationEndDate = BarrierObservationEndDate;
            o.BarrierObservationStartDate = BarrierObservationStartDate;
            o.BarrierObservationType = BarrierObservationType;
            o.BarrierSide = BarrierSide;
            return o;
        }

        public new IAssetInstrument SetStrike(double strike)
        {
            var o = (EuropeanBarrierOption)base.SetStrike(strike);
            o.Barrier = Barrier;
            o.BarrierObservationEndDate = BarrierObservationEndDate;
            o.BarrierObservationStartDate = BarrierObservationStartDate;
            o.BarrierObservationType = BarrierObservationType;
            o.BarrierSide = BarrierSide;
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
    }
}
