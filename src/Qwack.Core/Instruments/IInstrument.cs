using System;
using System.Collections.Generic;
using Qwack.Core.Basic;

namespace Qwack.Core.Instruments
{
    public interface IInstrument
    {
        string TradeId { get; }
        string Counterparty { get; set; }
        string PortfolioName { get; set; }
        DateTime LastSensitivityDate { get; }
        Currency Currency { get; }
        Dictionary<string, string> MetaData { get; set; }
    }
}
