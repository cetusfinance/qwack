using ExcelDna.Integration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Qwack.Excel.Services
{
    public static class ExcelHelper
    {
        private static EventId _eventId = new EventId(1);

        public static object Execute(ILogger logger, Func<object> functionToRun)
        {
            if (ExcelDnaUtil.IsInFunctionWizard()) return "Disabled in function wizard";

            try
            {
                return functionToRun.Invoke();
            }
            catch (Exception ex)
            {
                logger?.LogWarning(_eventId, ex, "Unhandled exception");
                return ex.Message;
            }
        }

        public static DateTime[] ToDateTimeArray(this IEnumerable<double> datesAsDoubles)
        {
            return datesAsDoubles.Select(DateTime.FromOADate).ToArray();
        }
    }
}
