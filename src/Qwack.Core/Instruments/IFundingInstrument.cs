using Qwack.Core.Models;

namespace Qwack.Core.Instruments
{
    public interface IFundingInstrument
    {
        double Pv(FundingModel model, bool updateState);
        string SolveCurve { get; set; }
        CashFlowSchedule ExpectedCashFlows(FundingModel model);
    }
}
