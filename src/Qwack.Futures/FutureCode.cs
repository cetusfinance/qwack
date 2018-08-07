using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qwack.Futures
{
    public class FutureCode
    {
        private FutureSettings _settings;
        private static readonly string[] s_futureMonths = new string[] { "F", "G", "H", "J", "K", "M", "N", "Q", "U", "V", "X", "Z" };

        public int YearBeforeWhich2DigitDatesAreUsed { get; set; }
        public string Code { get; set; }
        public int Year { get; set; }
        public string YearCode { get; set; }
        public string MonthCode { get; set; }
        public int Month { get; set; }
        public string ContractCode { get; set; }
        public string Prefix { get; set; }
        public string Postfix { get; set; }
        public string OriginalCode { get; set; }
        public int YearNumber { get; set; }
        public int MonthNumber { get; set; }
        public int MonthNumber12 { get; set; }

        public FutureSettings Settings => _settings;

        public FutureCode(string futureCodeRoot, IFutureSettingsProvider futureSettings) => _settings = futureSettings[futureCodeRoot];

        public FutureCode(string futureCode, int yearBeforeWhich2DigitDatesAreUsed, IFutureSettingsProvider futureSettings)
        {
            OriginalCode = futureCode;
            YearBeforeWhich2DigitDatesAreUsed = yearBeforeWhich2DigitDatesAreUsed;

            //Find the last number
            var i = 0;
            int tempNumber;

            for (i = futureCode.Length; i >= 0; i--)
            {
                if (int.TryParse(futureCode[i - 1].ToString(), out tempNumber))
                {
                    break;
                }
            }

            //Now we have the index we can hack the end off
            Postfix = futureCode.Substring(i);
            futureCode = futureCode.Substring(0, i);

            //Find when the numbers end

            for (i = futureCode.Length; i >= 0; i--)
            {
                if (!int.TryParse(futureCode[i - 1].ToString(), out tempNumber))
                    break;
            }

            YearCode = futureCode.Substring(i);
            YearNumber = int.Parse(YearCode);

            futureCode = futureCode.Substring(0, i);

            MonthCode = futureCode[futureCode.Length - 1].ToString();
            Prefix = futureCode.Substring(0, futureCode.Length - 1);

            if (!futureSettings.TryGet(Prefix, out _settings))
            {
                ContractCode = Prefix.ToUpper().Trim();
                _settings = futureSettings[ContractCode];
            }

            MonthNumber12 = s_futureMonths.ToList().IndexOf(MonthCode) + 1;
            MonthNumber = _settings.Months.IndexOf(MonthCode) + 1;
            ConvertYearCode();
        }

        public string GetPreviousCode()
        {
            //We need to move back 2 months because the month number starts at 1 but our list is 0 indexed
            var monthIndex = _settings.Months.IndexOf(MonthCode) - 1;
            var yearNumber = YearNumber;
            if (monthIndex < 0)
            {
                //We are wrapping over the end of year
                monthIndex = monthIndex + _settings.Months.Count;
                yearNumber--;
            }

            //Now we need to figure out if we are using 1 digit or 2 digit year codes or 4 digit
            string futureCode;
            if (yearNumber > 1000)
            {
                futureCode = Prefix + _settings.Months[monthIndex] + yearNumber.ToString() + Postfix;
                return futureCode;
            }

            if (YearBeforeWhich2DigitDatesAreUsed < 10)
            {
                yearNumber = yearNumber <= YearBeforeWhich2DigitDatesAreUsed ? yearNumber - 2010 : yearNumber - 2000;
            }
            else
            {
                yearNumber = yearNumber <= (YearBeforeWhich2DigitDatesAreUsed - 10) ? yearNumber - 2020 : yearNumber - 2010;
            }

            //now we have the year number sorted we can put it all together
            //Prefix then the month code, and then the year code and finally any postfix
            futureCode = Prefix + _settings.Months[monthIndex] + yearNumber.ToString() + Postfix;

            return futureCode;
        }

        public string GetNextCode(bool return4Digits)
        {
            var monthIndex = _settings.Months.IndexOf(MonthCode) + 1;
            var yearNumber = YearNumber;
            if (MonthNumber == _settings.Months.Count)
            {
                //We are wrapping over the end of year
                monthIndex -= _settings.Months.Count;
                yearNumber++;
            }

            if (return4Digits)
            {
                return Prefix + _settings.Months[monthIndex] + yearNumber.ToString() + Postfix;

            }

            //Now we need to figure out if we are using 1 digit or 2 digit year codes

            if (YearBeforeWhich2DigitDatesAreUsed < 10)
            {
                yearNumber = yearNumber <= YearBeforeWhich2DigitDatesAreUsed ? yearNumber - 2010 : yearNumber - 2000;
            }
            else
            {
                yearNumber = yearNumber <= (YearBeforeWhich2DigitDatesAreUsed - 10) ? yearNumber - 2020 : yearNumber - 2010;
            }

            //now we have the year number sorted we can put it all together
            //Prefix then the month code, and then the year code and finally any postfix
            var futureCode = Prefix + _settings.Months[monthIndex] + yearNumber.ToString() + Postfix;

            return futureCode;
        }

        private void ConvertYearCode()
        {
            var YearNumber = int.Parse(YearCode);
            if (YearNumber > 1000)
            {
                return;
            }
            if (YearNumber > 9)
            {
                //We have a 2 digit year
                YearNumber = 2000 + YearNumber;
            }
            else
            {
                if (YearBeforeWhich2DigitDatesAreUsed < 10)
                {
                    YearNumber = YearNumber <= YearBeforeWhich2DigitDatesAreUsed ? 2010 + YearNumber : 2000 + YearNumber;
                }
                else
                {
                    YearNumber = YearNumber <= (YearBeforeWhich2DigitDatesAreUsed - 10) ? 2020 + YearNumber : 2010 + YearNumber;
                }
            }
        }
    }
}
