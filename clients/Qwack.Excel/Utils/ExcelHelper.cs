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

        public static T OptionalExcel<T>(this object objectInput, T defaultValue)
        {
            T returnValue = defaultValue;
            if (!(objectInput is ExcelMissing))
                returnValue = (T)objectInput;

            return returnValue;
        }

        public static object ReturnExcelRangeVector(this object[] data)
        {
            ExcelReference caller = (ExcelReference)XlCall.Excel(XlCall.xlfCaller);
            // Now you can inspect the size of the caller with 
            int rows = caller.RowLast - caller.RowFirst + 1;
            int cols = caller.ColumnLast - caller.ColumnFirst + 1;

            if(rows>cols) //return column vector
            {
                var o = new object[data.Length, 1];
                for(int r=0;r<o.Length;r++)
                {
                    o[r, 0] = data[r];
                }
                return o;
            }
            else //return row vector == default behaviour
            {
                return data;
            }
        }
    }
}
