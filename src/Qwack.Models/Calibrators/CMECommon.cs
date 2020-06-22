using System;
using System.Collections.Generic;
using System.Text;
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

        public static (OptionExerciseType optionExerciseType, OptionMarginingType optionMarginingType) OptionTypeFromCode(string code)
        {
            switch (code)
            {
                case "CL":
                case "LO":
                case "PO":
                case "PAO":
                case "ON":
                case "OH":
                case "OB":
                case "NG":
                    return (OptionExerciseType.American, OptionMarginingType.Regular);
                case "CO":
                case "BZO":
                    return (OptionExerciseType.American, OptionMarginingType.FuturesStyle);
                case "AO":
                case "BA":
                    return (OptionExerciseType.Asian, OptionMarginingType.Regular);
                default:
                    throw new Exception($"No option style mapping found for {code}");
            }
        }

        public static DateTime OptionExpiryFromNymexRecord(NYMEXOptionRecord record, ICalendarProvider calendarProvider)
        {
            switch (record.Symbol)
            {
                case "LO": //WTI American
                    return new DateTime(record.ContractYear, record.ContractMonth, 26)
                        .AddMonths(-1)
                        .SubtractPeriod(RollType.P, calendarProvider.Collection["NYC"], 7.Bd());
                case "ON": //HH Natgas
                case "OH": //Heat
                case "OB": //Heat
                    return new DateTime(record.ContractYear, record.ContractMonth, 1)
                        .SubtractPeriod(RollType.P, calendarProvider.Collection["NYC"], 4.Bd());
                case "BZO": //NYMEX Brent
                    return new DateTime(record.ContractYear, record.ContractMonth, 1)
                        .AddMonths(-1)
                        .SubtractPeriod(RollType.P, calendarProvider.Collection["LON"], 4.Bd());
                default:
                    throw new Exception($"No option expiry mapping found for {record.Symbol}");
            }
        }
    }
}
