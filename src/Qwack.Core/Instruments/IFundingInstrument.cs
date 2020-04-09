using Qwack.Core.Models;
using System.Collections.Generic;
using System;
using Qwack.Core.Basic;

namespace Qwack.Core.Instruments
{
    public interface IFundingInstrument : IInstrument
    {
        DateTime PillarDate { get; set; }
        Currency Currency { get; }
        double Pv(IFundingModel model, bool updateState);
        double CalculateParRate(IFundingModel model);
        string SolveCurve { get; set; }
        Dictionary<string, Dictionary<DateTime, double>> Sensitivities(IFundingModel model);
        List<string> Dependencies(IFxMatrix matrix);
        IFundingInstrument Clone();
        IFundingInstrument SetParRate(double parRate);

        List<CashFlow> ExpectedCashFlows(IAssetFxModel model);

        double SuggestPillarValue(IFundingModel assetFxModel);
    }
}
