using ExcelDna.Integration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Qwack.Dates;

namespace Qwack.Excel.Services
{
    public static class ExcelHelper
    {
        private static readonly EventId _eventId = new EventId(1);

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

        public static DateTime[] ToDateTimeArray(this IEnumerable<double> datesAsDoubles) => datesAsDoubles.Select(DateTime.FromOADate).ToArray();

        public static T[][] SquareToJagged<T>(this T[,] data)
        {
            var o = new T[data.GetLength(0)][];
            for(var r=0;r< data.GetLength(0);r++)
            {
                o[r] = new T[data.GetLength(1)];
                for(var c=0;c<data.GetLength(1);c++)
                {
                    o[r][c] = data[r, c];
                }
            }
            return o;
        }

        public static Dictionary<T1, T2> RangeToDictionary<T1, T2>(this object[,] input)
        {
            if (input.GetLength(1) != 2)
                throw new Exception("Expected Nx2 range for dictionary");

            var o = new Dictionary<T1, T2>();
            for (var r = 0; r < input.GetLength(0); r++)
            {
                T1 val1;
                T2 val2;

                if (typeof(T1).IsEnum)
                    val1 = (T1)Enum.Parse(typeof(T1), (string)input[r, 0]);
                else if (typeof(T1) == typeof(DateTime) && input[r, 0] is double)
                {
                    val1 = (T1)((object)DateTime.FromOADate((double)input[r, 0]));
                }
                else
                    val1 = (T1)Convert.ChangeType(input[r, 0], typeof(T1));

                if (typeof(T2).IsEnum)
                    val2 = (T2)Enum.Parse(typeof(T2), (string)input[r, 1]);
                else if (typeof(T2) == typeof(DateTime) && input[r, 1] is double)
                {
                    val2 = (T2)((object)DateTime.FromOADate((double)input[r, 1]));
                }
                else
                    val2 = (T2)Convert.ChangeType(input[r, 1], typeof(T2));

                o.Add(val1, val2);
            }
            return o;
        }

        public static List<KeyValuePair<T1, T2>> RangeToKvList<T1, T2>(this object[,] input)
        {
            if (input.GetLength(1) != 2)
                throw new Exception("Expected Nx2 range for input data");

            var o = new List<KeyValuePair<T1, T2>>();
            for (var r = 0; r < input.GetLength(0); r++)
            {
                T1 val1;
                T2 val2;

                if (typeof(T1).IsEnum)
                    val1 = (T1)Enum.Parse(typeof(T1), (string)input[r, 0]);
                else if (typeof(T1) == typeof(DateTime) && input[r, 0] is double)
                {
                    val1 = (T1)((object)DateTime.FromOADate((double)input[r, 0]));
                }
                else
                    val1 = (T1)Convert.ChangeType(input[r, 0], typeof(T1));

                if (typeof(T2).IsEnum)
                    val2 = (T2)Enum.Parse(typeof(T2), (string)input[r, 1]);
                else if (typeof(T2) == typeof(DateTime) && input[r, 1] is double)
                {
                    val2 = (T2)((object)DateTime.FromOADate((double)input[r, 1]));
                }
                else
                    val2 = (T2)Convert.ChangeType(input[r, 1], typeof(T2));

                o.Add(new KeyValuePair<T1, T2>(val1, val2));
            }
            return o;
        }

        public static T OptionalExcel<T>(this object objectInput, T defaultValue)
        {
            var returnValue = defaultValue;
            if (!(objectInput is ExcelMissing))
                returnValue = (T)objectInput;

            return returnValue;
        }

        public static object ReturnExcelRangeVector(this object[] data)
        {
            var caller = (ExcelReference)XlCall.Excel(XlCall.xlfCaller);
            // Now you can inspect the size of the caller with 
            var rows = caller.RowLast - caller.RowFirst + 1;
            var cols = caller.ColumnLast - caller.ColumnFirst + 1;

            if(rows>cols) //return column vector
            {
                var o = new object[data.Length, 1];
                for(var r=0;r<o.Length;r++)
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

        public static object ReturnExcelRangeVectorFromDouble(this double[] data)
        {
            ExcelReference caller;
            try
            {
                caller = (ExcelReference)XlCall.Excel(XlCall.xlfCaller);
            }
            catch
            {
                return data;
            }

            // Now you can inspect the size of the caller with 
            var rows = caller.RowLast - caller.RowFirst + 1;
            var cols = caller.ColumnLast - caller.ColumnFirst + 1;

            if (rows > cols) //return column vector
            {
                var o = new object[data.Length, 1];
                for (var r = 0; r < o.Length; r++)
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

        public static IEnumerable<T> GetAnyFromCache<T>(this object[] Names)
        {
            var tCache = ContainerStores.GetObjectCache<T>();
            var tS = Names
                .Where(s => !(s is ExcelMissing) && !(s is ExcelEmpty) && !string.IsNullOrWhiteSpace(s as string) && tCache.Exists(s as string))
                .Select(s => tCache.GetObject(s as string).Value);

            return tS;
        }

        public static T[] ObjectRangeToVector<T>(this object[,] input)
        {      
            if(input.GetLength(0)> input.GetLength(1))
            {
                var o = new T[input.GetLength(0)];
                for(var i=0;i<input.GetLength(0);i++)
                {
                    o[i] = input[i, 0] is ExcelEmpty ? default : (T)Convert.ChangeType(input[i, 0],typeof(T));
                }
                return o;
            }
            else
            {
                var o = new T[input.GetLength(1)];
                for (var i = 0; i < input.GetLength(1); i++)
                {
                    o[i] = input[0, i] is ExcelEmpty ? default : (T)Convert.ChangeType(input[0, i], typeof(T));
                }
                return o;
            }
        }

        public static T[,] ObjectRangeToMatrix<T>(this object[,] input)
        {
            var o = new T[input.GetLength(0), input.GetLength(1)];
            for (var i = 0; i < input.GetLength(0); i++)
            {
                for (var j = 0; j < input.GetLength(1); j++)
                {
                    o[i,j] = input[i, j] is ExcelEmpty ? default : (T)Convert.ChangeType(input[i, j], typeof(T));
                }
            }
            return o;
        }

        public static T[] ObjectRangeToVector<T>(this object[] input)
        {
            var o = new T[input.Length];
            for (var i = 0; i < input.Length; i++)
            {
                o[i] = input[i] is ExcelEmpty ? default : (T)Convert.ChangeType(input[i], typeof(T));
            }
            return o;
        }

        public static object ReturnPrettyExcelRangeVector(this object[,] data)
        {
            var caller = (ExcelReference)XlCall.Excel(XlCall.xlfCaller);
            // Now you can inspect the size of the caller with 
            var rows = caller.RowLast - caller.RowFirst + 1;
            var cols = caller.ColumnLast - caller.ColumnFirst + 1;

            var o = new object[rows, cols];

            for(var r=0;r<o.GetLength(0);r++)
                for(var c=0;c<o.GetLength(1);c++)
                {
                    if (r < data.GetLength(0) && c < data.GetLength(1))
                        o[r, c] = data[r, c];
                    else
                        o[r, c] = string.Empty;
                }

            return o;
        }
    }
}
