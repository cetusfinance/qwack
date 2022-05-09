using System;
using Qwack.Transport.BasicTypes;

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
