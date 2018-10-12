using System;
using System.Collections.Generic;
using System.Text;

namespace Qwack.Core.Instruments
{
    public interface IInstrument
    {
        string TradeId { get; }
        string Counterparty { get; set; }
    }
}
