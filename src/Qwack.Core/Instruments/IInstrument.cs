using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Models;

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
