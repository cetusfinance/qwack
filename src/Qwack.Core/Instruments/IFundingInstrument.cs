using Qwack.Core.Models;
using System.Collections.Generic;
using System;

namespace Qwack.Core.Instruments
{
    public interface IFundingInstrument
    {
        DateTime PillarDate { get; set; }
        double Pv(IFundingModel model, bool updateState);
        string SolveCurve { get; set; }
        CashFlowSchedule ExpectedCashFlows(IFundingModel model);
        Dictionary<string, Dictionary<DateTime, double>> Sensitivities(IFundingModel model);
    }
}
