using Qwack.Core.Models;
using System.Collections.Generic;
using System;

namespace Qwack.Core.Instruments
{
    public interface IFundingInstrument : IInstrument
    {
        DateTime PillarDate { get; set; }
        double Pv(IFundingModel model, bool updateState);
        double CalculateParRate(IFundingModel model);
        string SolveCurve { get; set; }
        CashFlowSchedule ExpectedCashFlows(IFundingModel model);
        Dictionary<string, Dictionary<DateTime, double>> Sensitivities(IFundingModel model);
        List<string> Dependencies(IFxMatrix matrix);
        IFundingInstrument Clone();
        IFundingInstrument SetParRate(double parRate);
    }
}
