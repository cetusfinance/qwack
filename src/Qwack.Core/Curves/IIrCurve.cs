using System;
using System.Collections.Generic;
using Qwack.Core.Basic;
using Qwack.Core.Descriptors;
using Qwack.Dates;

namespace Qwack.Core.Curves
{
    public interface IIrCurve : IHasDescriptors
    {
        DateTime BuildDate { get; }
        double GetDf(DateTime startDate, DateTime endDate);
        double GetForwardRate(DateTime startDate, DateTime endDate, RateType rateType, DayCountBasis basis);
        double GetForwardRate(DateTime startDate, DateTime endDate, RateType rateType, double tbasis);
        string Name { get; }
        IIrCurve SetRate(int pillarIx, double rate, bool mutate);
        int NumberOfPillars { get; }
        double GetRate(int pillarIx);
        double GetRate(DateTime valueDate);
        double[] GetSensitivity(DateTime valueDate);
        IrCurve BumpRate(int pillarIx, double delta, bool mutate);
        IrCurve BumpRateFlat(double delta, bool mutate);

        Dictionary<DateTime, IrCurve> BumpScenarios(double delta, DateTime lastSensitivityDate);
    }
}
