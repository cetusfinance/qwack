using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Core.Models;
using Qwack.Dates;

namespace Qwack.Core.Instruments.Asset
{
    public class AsianOption : AsianSwap
    {
        public OptionType CallPut { get; set; }

        public new IAssetInstrument Clone()
        {
            var o = (AsianOption)base.Clone();
            o.CallPut = CallPut;
            return o;
        }

        public new IAssetInstrument SetStrike(double strike)
        {
            var o = (AsianOption)base.SetStrike(strike);
            o.CallPut = CallPut;
            return o;
        }
    }
}
