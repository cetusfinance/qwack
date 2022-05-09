using System;
using System.Collections.Generic;
using System.Linq;
using Qwack.Dates;
using Qwack.Providers.CSV;
using Qwack.Transport.BasicTypes;

namespace Qwack.Models.Calibrators
{
    public static class CMECommon
    {
        public static double MinPrice = 1e-10;

        public static string Year2to1(string input)
        {
            if (int.Parse(input.Substring(input.Length - 2)) > DateTime.Today.Year - 2000 + 8)
                return input;
            else
                return input.Substring(0, input.Length - 2) + input.Substring(input.Length - 1);
        }

        public static string GetCode(string code, string MMY)
        {
            var m = int.Parse(MMY.Substring(MMY.Length - 2, 2));
            return $"{code}{DateExtensions.FutureMonths[m - 1]}{MMY.Substring(2, 2)}";
        }

        public static (OptionExerciseType optionExerciseType, OptionMarginingType optionMarginingType) OptionTypeFromCode(string code) => code switch
        {
            "CO" or "BZO" => (OptionExerciseType.American, OptionMarginingType.FuturesStyle),
            "AO" or "BA" => (OptionExerciseType.Asian, OptionMarginingType.Regular),
            _ => (OptionExerciseType.American, OptionMarginingType.Regular),
        };

        public static DateTime OptionExpiryFromNymexRecord(NYMEXOptionRecord record, ICalendarProvider calendarProvider) => record.Symbol switch
        {
            //WTI American
            "LO" => new DateTime(record.ContractYear, record.ContractMonth, 26)
                                    .AddMonths(-1)
                                    .SubtractPeriod(RollType.P, calendarProvider.Collection["NYC"], 7.Bd()),
            //HH Natgas
            "ON" or "OH" or "OB" => new DateTime(record.ContractYear, record.ContractMonth, 1)
                                    .SubtractPeriod(RollType.P, calendarProvider.Collection["NYC"], 4.Bd()),
            //NYMEX Brent
            "BZO" => new DateTime(record.ContractYear, record.ContractMonth, 1)
                                    .AddMonths(-1)
                                    .SubtractPeriod(RollType.P, calendarProvider.Collection["LON"], 4.Bd()),
            _ => throw new Exception($"No option expiry mapping found for {record.Symbol}"),
        };

        public static Dictionary<DateTime, double> Downsample(Dictionary<DateTime, double> curvePoints, DateTime valDate, Calendar calendar)
        {
            var tenors = new[] { "1b", "1w", "2w", "3w", "1m", "2m", "3m", "4m", "5m", "6m", "9m", "12m", "15m", "18m", "24m", "30m", "36m", "42m", "48m", "54m", "60m" };
            var dates = tenors.Select(t => valDate.AddPeriod(t.EndsWith("m") ? RollType.MF : RollType.F, calendar, new Frequency(t)));
            var p = curvePoints.Keys.OrderBy(x => x).ToList();
            var ixs = dates.Select(d => p.BinarySearch(d)).Select(x => x < 0 ? ~x : x).Where(x => x < p.Count()).Distinct().ToArray();
            var smaller = ixs.ToDictionary(x => p[x], x => curvePoints[p[x]]);
            return smaller;
        }
    }
}
