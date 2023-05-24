using System;
using System.Collections.Generic;
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
    }
}
