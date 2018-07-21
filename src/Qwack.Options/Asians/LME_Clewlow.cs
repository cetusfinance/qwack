using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Dates;
using Qwack.Core.Basic;
using System.Linq;

namespace Qwack.Options.Asians
{
    public static class LME_Clewlow
    {
        public static double PV(double forward, double knownAverage, double sigma, double K, DateTime evalDate, DateTime avgStartDate, DateTime avgEndDate, double riskFree, OptionType callPut, Calendar fixingCalendar)
        {
            if (avgEndDate == evalDate)
                if (callPut == OptionType.C)
                    return System.Math.Max(knownAverage - K, 0);
                else
                    return System.Math.Max(K - knownAverage, 0);

            else if (avgEndDate < evalDate)
                return 0;
            else if (avgStartDate == avgEndDate)
            {
                var t = (avgEndDate - evalDate).TotalDays / 365.0;
                return BlackFunctions.BlackPV(forward, K, riskFree, t, sigma, callPut);
            }

            //Build Vector of Observation Dates
            var resetDates = avgStartDate.BusinessDaysInPeriod(avgEndDate, fixingCalendar);
            var RT = resetDates.Count;

            //Initialise Variables
            double E1 = 0.0, E2 = 0.0, E3 = 0.0, E4 = 0.0, E5 = 0.0;
            double DeltaTPrime = 0.0, DeltaT = 0.0;

            var FBar = forward;
            var tvs = resetDates.Count(x => x < evalDate);

            if (tvs > 0)
                DeltaTPrime = (resetDates[tvs + 1] - resetDates[tvs]).TotalDays / 365.0;
            else
                DeltaTPrime = ((resetDates[0] - evalDate).TotalDays) / 365.0;

            if (tvs + 2 >= RT)
                DeltaT = DeltaTPrime;
            else
                DeltaT = (avgEndDate - resetDates[tvs+1]).TotalDays / 365.0 / (RT - tvs);

            var Ak = knownAverage * tvs  / RT;
            E5 = 2 * Ak * FBar * (RT - tvs) / RT + (Ak * Ak);

            if (tvs < RT)
            {
                E4 = (1 + System.Math.Exp(sigma * sigma * DeltaT)) * (System.Math.Exp((RT - tvs) * sigma * sigma * DeltaT) - 1);
                E4 += 2 * (RT - tvs) * (1 - System.Math.Exp(sigma * sigma * DeltaT));
                E4 /= (System.Math.Pow(System.Math.Exp(sigma * sigma * DeltaT) - 1, 2));
            }
            else
                E4 = 1;


            E3 = System.Math.Pow(FBar / RT, 2) * System.Math.Exp(sigma * sigma * DeltaTPrime);
            E2 = E3 * E4 + E5;
            E1 = FBar * (RT - tvs) / RT  + Ak;

            var EA = Ak + FBar * (RT - tvs) / RT;

            var b = System.Math.Log(E2) - 2 * System.Math.Log(E1);
            var d1 = (System.Math.Log(EA / K) + 0.5 * b) / System.Math.Sqrt(b);
            var d2 = d1 - System.Math.Sqrt(b);


            var df = System.Math.Exp(-riskFree * (avgEndDate - evalDate).TotalDays / 365.0);

            //Main Option valuation
            if (callPut == OptionType.Call)
            {
                d1 = Math.Statistics.NormSDist(d1);
                d2 = Math.Statistics.NormSDist(d2);
                return (EA * d1 - K * d2) * df;
            }
            else
            {
                d1 = Math.Statistics.NormSDist(-d1);
                d2 = Math.Statistics.NormSDist(-d2);
                return (K * d2 - EA * d1) * df;
            }

        }
    }
}
