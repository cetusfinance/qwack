using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Qwack.Core.Models;

namespace Qwack.Core.Instruments.Asset
{
    public class AsianSwapStrip : IInstrument
    {
        public string TradeId { get; set; }

        public AsianSwap[] Swaplets { get; set; }
    }
}
