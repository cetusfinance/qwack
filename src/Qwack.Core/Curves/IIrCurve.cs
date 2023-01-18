using System;
using System.Collections.Generic;
using Qwack.Transport.BasicTypes;

namespace Qwack.Core.Curves
{
    public interface IIrCurve
    {
        DateTime BuildDate { get; }
        double GetDf(DateTime startDate, DateTime endDate);
        double GetDf(double tStart, double tEnd);
        double GetForwardRate(DateTime startDate, DateTime endDate, RateType rateType, DayCountBasis basis);
        double GetForwardRate(DateTime startDate, DateTime endDate, RateType rateType, double tbasis);
        string Name { get; }
        IIrCurve SetRate(int pillarIx, double rate, bool mutate);
        int NumberOfPillars { get; }
        double GetRate(int pillarIx);
        double GetRate(DateTime valueDate);
        double[] GetSensitivity(DateTime valueDate);
        IIrCurve BumpRate(int pillarIx, double delta, bool mutate);
        IIrCurve BumpRateFlat(double delta, bool mutate);

        Dictionary<DateTime, IIrCurve> BumpScenarios(double delta, DateTime lastSensitivityDate);

        DayCountBasis Basis { get; }

        IIrCurve RebaseDate(DateTime newAnchorDate);
    }
}
