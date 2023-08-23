using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Qwack.Core.Models;
using Qwack.Math;

namespace Qwack.Core.Instruments.Funding
{
    public static class InflationUtils
    {
        public static double InterpFixing(DateTime fixingDate, double indexA, double indexB)
        {
            var d = fixingDate.Day;
            var som = new DateTime(fixingDate.Year, fixingDate.Month, 1);
            var D = som.AddMonths(1).Subtract(som).TotalDays;

            return indexA + d / D * (indexB - indexA);
        }

        public static double InterpFixing(DateTime fixingDate, IInterpolator1D fixings, int fixingLagMonths)
        {
            var d0 = fixingDate.AddMonths(-System.Math.Abs(fixingLagMonths));
            var indexA = fixings.Interpolate(new DateTime(d0.Year, d0.Month, 1).ToOADate());
            var indexB = fixings.Interpolate(new DateTime(d0.Year, d0.Month, 1).AddMonths(1).ToOADate());
            return InterpFixing(fixingDate, indexA, indexB);  
        }

        public static double InterpFixing(DateTime fixingDate, Func<DateTime, double> getForecastLevel, int fixingLagMonths)
        {
            var d0 = fixingDate.AddMonths(-System.Math.Abs(fixingLagMonths));
            var indexA = getForecastLevel(new DateTime(d0.Year, d0.Month, 1));
            var indexB = getForecastLevel(new DateTime(d0.Year, d0.Month, 1).AddMonths(1));
            return InterpFixing(fixingDate, indexA, indexB);
        }

        public static double InterpFixing(DateTime fixingDate, IFixingDictionary fixings, int fixingLagMonths)
        {
            var d0 = fixingDate.AddMonths(-System.Math.Abs(fixingLagMonths));
            var som = new DateTime(d0.Year, d0.Month, 1);
            var indexA = fixings[som];
            if (som == d0)
                return indexA;
            var indexB = fixings[som.AddMonths(1)];
            return InterpFixing(fixingDate, indexA, indexB);
        }

        public static double[] ImplySeasonality(this IFixingDictionary fixings, int nYears)
        {
            var latestFixing = fixings.Keys.Max();
            var d = latestFixing.AddYears(-nYears);
            var monthTotals = new Dictionary<int, double>();
            while(d<latestFixing) 
            {
                var f0 = fixings[d];
                d = d.AddMonths(1);
                var f1 = fixings[d];
                var m = d.Month;
                if(!monthTotals.ContainsKey(m))
                    monthTotals[m] = 0;
                monthTotals[m] += System.Math.Log(f1 / f0) / nYears;
            }

            var avg = monthTotals.Values.Average();

            return Enumerable.Range(1,12).Select(i => monthTotals[i] - avg).ToArray();
        }
    }
}
