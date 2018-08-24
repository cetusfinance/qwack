using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExcelDna.Integration;
using Qwack.Core.Curves;
using Qwack.Excel.Services;
using Qwack.Excel.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Qwack.Math.Interpolation;
using Qwack.Core.Basic;
using Qwack.Dates;
using Qwack.Futures;

namespace Qwack.Excel.Dates
{
    public class FuturesFunctions
    {
        private static readonly ILogger _logger = ContainerStores.GlobalContainer.GetService<ILoggerFactory>()?.CreateLogger<FuturesFunctions>();

        [ExcelFunction(Description = "Returns expiry date for a given futures code", Category = CategoryNames.Dates, Name = CategoryNames.Dates + "_" + nameof(FuturesExpiryFromCode))]
        public static object FuturesExpiryFromCode(
             [ExcelArgument(Description = "Futures code, e.g. CLZ3")] string FuturesCode)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var c = new FutureCode(FuturesCode, DateTime.Today.Year - 2, ContainerStores.SessionContainer.GetService<IFutureSettingsProvider>());

                return c.GetExpiry();
            });
        }

        [ExcelFunction(Description = "Returns roll date for a given futures code", Category = CategoryNames.Dates, Name = CategoryNames.Dates + "_" + nameof(FuturesRollFromCode))]
        public static object FuturesRollFromCode(
            [ExcelArgument(Description = "Futures code, e.g. CLZ3")] string FuturesCode)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var c = new FutureCode(FuturesCode, DateTime.Today.Year - 2, ContainerStores.SessionContainer.GetService<IFutureSettingsProvider>());

                return c.GetRollDate();
            });
        }

        [ExcelFunction(Description = "Returns front month code for a given futures root and value date", Category = CategoryNames.Dates, Name = CategoryNames.Dates + "_" + nameof(FuturesGetFrontMonth))]
        public static object FuturesGetFrontMonth(
            [ExcelArgument(Description = "Value date")] DateTime ValueDate,
            [ExcelArgument(Description = "Futures code root, e.g. CL")] string FuturesCodeRoot)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var dummyFutureCode = $"{FuturesCodeRoot}Z{DateExtensions.SingleDigitYear(DateTime.Today.Year + 2)}";

                var c = new FutureCode(dummyFutureCode, DateTime.Today.Year - 2, ContainerStores.SessionContainer.GetService<IFutureSettingsProvider>());

                return c.GetFrontMonth(ValueDate);
            });
        }

        [ExcelFunction(Description = "Returns next code in expiry sequence from a given code", Category = CategoryNames.Dates, Name = CategoryNames.Dates + "_" + nameof(FuturesNextCode))]
        public static object FuturesNextCode(
        [ExcelArgument(Description = "Futures code, e.g. CLZ3")] string FuturesCode)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var c = new FutureCode(FuturesCode, DateTime.Today.Year - 2, ContainerStores.SessionContainer.GetService<IFutureSettingsProvider>());

                return c.GetNextCode(false);
            });
        }

        [ExcelFunction(Description = "Returns previous code in expiry sequence from a given code", Category = CategoryNames.Dates, Name = CategoryNames.Dates + "_" + nameof(FuturesPreviousCode))]
        public static object FuturesPreviousCode(
        [ExcelArgument(Description = "Futures code, e.g. CLZ3")] string FuturesCode)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                var c = new FutureCode(FuturesCode, DateTime.Today.Year - 2, ContainerStores.SessionContainer.GetService<IFutureSettingsProvider>());

                return c.GetPreviousCode();
            });
        }

        [ExcelFunction(Description = "Generates next LME curve date", Category = CategoryNames.Dates, Name = CategoryNames.Dates + "_" + nameof(NextLMECurveDate))]
        public static object NextLMECurveDate(
            [ExcelArgument(Description = "Previous date")] DateTime PrevDate,
            [ExcelArgument(Description = "3m date")] DateTime ThreeMonthDate,
            [ExcelArgument(Description = "Whether all daily expiries are required")] bool DailyExpiries)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                ContainerStores.
                SessionContainer.
                GetService<ICalendarProvider>().
                Collection.
                TryGetCalendar("LON+NYC", out var cal);

                if (DailyExpiries)
                {
                    if (PrevDate < ThreeMonthDate)
                        return PrevDate.AddPeriod(RollType.F, cal, new Frequency(1, DatePeriodType.B));

                    if (PrevDate < ThreeMonthDate.AddMonths(3))
                        return PrevDate.GetNextWeekday(DayOfWeek.Wednesday).IfHolidayRollForward(cal);

                    if (PrevDate < PrevDate.ThirdWednesday())
                        return PrevDate.ThirdWednesday();

                    return PrevDate.AddMonths(1).ThirdWednesday();
                }
                else
                {
                    var next3w = (PrevDate < PrevDate.ThirdWednesday()) ? 
                                    PrevDate.ThirdWednesday() : 
                                    PrevDate.AddMonths(1).ThirdWednesday();

                    if (PrevDate < ThreeMonthDate && next3w > ThreeMonthDate)
                        return ThreeMonthDate;

                    return next3w;
                }

            });
        }

        [ExcelFunction(Description = "Generates LME 3m date", Category = CategoryNames.Dates, Name = CategoryNames.Dates + "_" + nameof(GetLME3mDate))]
        public static object GetLME3mDate(
            [ExcelArgument(Description = "Value date")] DateTime ValueDate)
        {
            return ExcelHelper.Execute(_logger, () =>
            {
                ContainerStores.
                SessionContainer.
                GetService<ICalendarProvider>().
                Collection.
                TryGetCalendar("LON+NYC", out var cal);

                return ValueDate.AddPeriod(RollType.LME_Nearest, cal, new Frequency(3, DatePeriodType.M));
            });
        }
    }
}
