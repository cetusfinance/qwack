using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Dates;

namespace Qwack.Core.Instruments.Asset
{
    public class FuturesOption : Future, IHasVega
    {
        public OptionType CallPut { get; set; }
        public OptionExerciseType ExerciseType { get; set; }
        public OptionMarginingType MarginingType { get; set; }
        public string DiscountCurve { get; set; }

        public new IAssetInstrument Clone()
        {
            var o = (FuturesOption)base.Clone();
            o.CallPut = CallPut;
            return o;
        }

        public new IAssetInstrument SetStrike(double strike)
        {
            var o = (FuturesOption)base.SetStrike(strike);
            o.CallPut = CallPut;
            return o;
        }
    }
}
