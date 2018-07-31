using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExcelDna.Integration;
using Qwack.Options;
using Qwack.Excel.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Qwack.Options.VolSurfaces;
using Qwack.Excel.Utils;
using Qwack.Dates;
using Qwack.Core.Basic;

namespace Qwack.Excel.Options
{
    public class AsianFunctions
    {
        private static readonly ILogger _logger = ContainerStores.GlobalContainer.GetService<ILoggerFactory>()?.CreateLogger<BlackFunctions>();

        [ExcelFunction(Description = "Returns asian option PV using the Turnbull-Wakeman formula", Category = CategoryNames.Options, Name = CategoryNames.Options + "_" + nameof(TurnbullWakemanPV))]
        public static object TurnbullWakemanPV(
            [ExcelArgument(Description = "Time-to-expiry")] double T,
            [ExcelArgument(Description = "Time-to-average start")] double TavgStart,
            [ExcelArgument(Description = "Average-to-date")] double KnownAverage,
            [ExcelArgument(Description = "Strike")] double K,
            [ExcelArgument(Description = "Forward")] double F,
            [ExcelArgument(Description = "Discounting rate")] double R,
            [ExcelArgument(Description = "Volatility")] double V,
            [ExcelArgument(Description = "Call or Put")] string CP)
        {
            return ExcelHelper.Execute(_logger, () =>
             {
                 if (!Enum.TryParse(CP, out OptionType optType))
                 {
                     return $"Could not parse call or put flag - {CP}";
                 }
                 return Qwack.Options.Asians.TurnbullWakeman.PV(F, KnownAverage, V, K, TavgStart, T, R, optType);
             });
        }

        [ExcelFunction(Description = "Returns asian option PV using the Turnbull-Wakeman formula", Category = CategoryNames.Options, Name = CategoryNames.Options + "_" + nameof(TurnbullWakemanPV2))]
        public static object TurnbullWakemanPV2(
            [ExcelArgument(Description = "Evaluation Date")] DateTime EvalDate,
            [ExcelArgument(Description = "Average Start Date")] DateTime AverageStartDate,
            [ExcelArgument(Description = "Average End Date")] DateTime AverageEndDate,
            [ExcelArgument(Description = "Average-to-date")] double KnownAverage,
            [ExcelArgument(Description = "Strike")] double K,
            [ExcelArgument(Description = "Forward")] double F,
            [ExcelArgument(Description = "Discounting rate")] double R,
            [ExcelArgument(Description = "Volatility")] double V,
            [ExcelArgument(Description = "Call or Put")] string CP)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                if (!Enum.TryParse(CP, out OptionType optType))
                {
                    return $"Could not parse call or put flag - {CP}";
                }
                return Qwack.Options.Asians.TurnbullWakeman.PV(F, KnownAverage, V, K, EvalDate, AverageStartDate, AverageEndDate, R, optType);
            });
        }

        [ExcelFunction(Description = "Returns asian option delta using the Turnbull-Wakeman formula", Category = CategoryNames.Options, Name = CategoryNames.Options + "_" + nameof(TurnbullWakemanDelta))]
        public static object TurnbullWakemanDelta(
            [ExcelArgument(Description = "Evaluation Date")] DateTime EvalDate,
            [ExcelArgument(Description = "Average Start Date")] DateTime AverageStartDate,
            [ExcelArgument(Description = "Average End Date")] DateTime AverageEndDate,
            [ExcelArgument(Description = "Average-to-date")] double KnownAverage,
            [ExcelArgument(Description = "Strike")] double K,
            [ExcelArgument(Description = "Forward")] double F,
            [ExcelArgument(Description = "Discounting rate")] double R,
            [ExcelArgument(Description = "Volatility")] double V,
            [ExcelArgument(Description = "Call or Put")] string CP)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                if (!Enum.TryParse(CP, out OptionType optType))
                {
                    return $"Could not parse call or put flag - {CP}";
                }
                return Qwack.Options.Asians.TurnbullWakeman.Delta(F, KnownAverage, V, K, EvalDate, AverageStartDate, AverageEndDate, R, optType);
            });
        }

        [ExcelFunction(Description = "Returns strike for asian option with specified PV, using the Turnbull-Wakeman formula", Category = CategoryNames.Options, Name = CategoryNames.Options + "_" + nameof(TurnbullWakemanStrikeForPV))]
        public static object TurnbullWakemanStrikeForPV(
            [ExcelArgument(Description = "Evaluation Date")] DateTime EvalDate,
            [ExcelArgument(Description = "Average Start Date")] DateTime AverageStartDate,
            [ExcelArgument(Description = "Average End Date")] DateTime AverageEndDate,
            [ExcelArgument(Description = "Average-to-date")] double KnownAverage,
            [ExcelArgument(Description = "Target PV")] double PV,
            [ExcelArgument(Description = "Forward")] double F,
            [ExcelArgument(Description = "Discounting rate")] double R,
            [ExcelArgument(Description = "Volatility Surface")] string VolSurface,
            [ExcelArgument(Description = "Call or Put")] string CP)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                if (!ContainerStores.GetObjectCache<IVolSurface>().TryGetObject(VolSurface, out var surface))
                {
                    return $"Could not parse find vol surface {VolSurface} in the cache";
                }

                if (!Enum.TryParse(CP, out OptionType optType))
                {
                    return $"Could not parse call or put flag - {CP}";
                }
                return Qwack.Options.Asians.TurnbullWakeman.StrikeForPV(PV, F, KnownAverage, surface.Value, EvalDate, AverageStartDate, AverageEndDate, R, optType);
            });
        }

        [ExcelFunction(Description = "Returns asian option PV using the Clewlow/LME formula", Category = CategoryNames.Options, Name = CategoryNames.Options + "_" + nameof(ClewlowPV))]
        public static object ClewlowPV(
            [ExcelArgument(Description = "Evaluation Date")] DateTime EvalDate,
            [ExcelArgument(Description = "Average Start Date")] DateTime AverageStartDate,
            [ExcelArgument(Description = "Average End Date")] DateTime AverageEndDate,
            [ExcelArgument(Description = "Average-to-date")] double KnownAverage,
            [ExcelArgument(Description = "Strike")] double K,
            [ExcelArgument(Description = "Forward")] double F,
            [ExcelArgument(Description = "Discounting rate")] double R,
            [ExcelArgument(Description = "Volatility")] double V,
            [ExcelArgument(Description = "Call or Put")] string CP,
            [ExcelArgument(Description = "Fixing Calendar")] object FixingCalendar)
        {

            return ExcelHelper.Execute(_logger, () =>
            {
                var fixCal = FixingCalendar.OptionalExcel<string>("Weekends");
                if (!ContainerStores.SessionContainer.GetService<ICalendarProvider>().Collection.TryGetCalendar(fixCal, out var cal))
                    return $"Calendar {FixingCalendar} not found in cache";

                if (!Enum.TryParse(CP, out OptionType optType))
                {
                    return $"Could not parse call or put flag - {CP}";
                }
                return Qwack.Options.Asians.LME_Clewlow.PV(F, KnownAverage, V, K, EvalDate, AverageStartDate, AverageEndDate, R, optType, cal);
            });
        }

        [ExcelFunction(Description = "Returns asian option delta using the Clewlow/LME formula", Category = CategoryNames.Options, Name = CategoryNames.Options + "_" + nameof(ClewlowDelta))]
        public static object ClewlowDelta(
            [ExcelArgument(Description = "Evaluation Date")] DateTime EvalDate,
            [ExcelArgument(Description = "Average Start Date")] DateTime AverageStartDate,
            [ExcelArgument(Description = "Average End Date")] DateTime AverageEndDate,
            [ExcelArgument(Description = "Average-to-date")] double KnownAverage,
            [ExcelArgument(Description = "Strike")] double K,
            [ExcelArgument(Description = "Forward")] double F,
            [ExcelArgument(Description = "Discounting rate")] double R,
            [ExcelArgument(Description = "Volatility")] double V,
            [ExcelArgument(Description = "Call or Put")] string CP,
            [ExcelArgument(Description = "Fixing Calendar")] object FixingCalendar)
        {

            return ExcelHelper.Execute(_logger, () =>
            {
                var fixCal = FixingCalendar.OptionalExcel<string>("Weekends");
                if (!ContainerStores.SessionContainer.GetService<ICalendarProvider>().Collection.TryGetCalendar(fixCal, out var cal))
                    return $"Calendar {FixingCalendar} not found in cache";

                if (!Enum.TryParse(CP, out OptionType optType))
                {
                    return $"Could not parse call or put flag - {CP}";
                }
                return Qwack.Options.Asians.LME_Clewlow.Delta(F, KnownAverage, V, K, EvalDate, AverageStartDate, AverageEndDate, R, optType, cal);
            });
        }
    }
}
