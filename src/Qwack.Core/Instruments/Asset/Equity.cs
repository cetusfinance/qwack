using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Transport.BasicTypes;

namespace Qwack.Core.Instruments.Asset
{
    public class Equity : CashAsset
    {
        public Equity() : base() { }
        public Equity(double notional, string assetId, Currency ccy, double scalingFactor, Frequency settleLag, Calendar settleCalendar)
            : base(notional, assetId, ccy, scalingFactor, settleLag, settleCalendar)
        {
        }
    }
}
