using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Dates;

namespace Qwack.Core.Instruments.Asset
{
    public class AsianOption : AsianSwap
    {
        public OptionType CallPut { get; set; }
    }
}
