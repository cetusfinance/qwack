using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Core.Basic;

namespace Qwack.Core.Trades
{
    public class ExecutedTrade
    {
        public decimal Size { get; set; }
        public decimal Price { get; set; }
        public Guid ProductSerialNumber { get; set; }
        public TradeDirection Direction { get; set; }
    }
}
