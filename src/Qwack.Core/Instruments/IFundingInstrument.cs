using Qwack.Core.Models;
using System.Collections.Generic;
using System;

namespace Qwack.Core.Instruments
{
    public interface IFundingInstrument
    {
        double Pv(FundingModel model, bool updateState);
        string SolveCurve { get; set; }
        CashFlowSchedule ExpectedCashFlows(FundingModel model);
        Dictionary<string, Dictionary<DateTime, double>> Sensitivities(FundingModel model);
    }
}
