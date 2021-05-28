using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Qwack.Futures
{
    public static class CodeConversion
    {
        public static string[] MonthCodes = new[] { "F", "G", "H", "K", "J", "M", "N", "Q", "U", "V", "X", "Z" };

        public static string ConvertBbgToIB(this string bbg, IFutureSettingsProvider provider)
        {
            switch (bbg.ToLower().Right(6))
            {
                case "comdty":
                    if(bbg.Length<13) //its not an option
                    {
                        if(bbg.Length==11 && bbg.Substring(4,1)==" ") // ccMY Comdty format
                        {
                            var bbgCode = bbg.Left(2);
                            if (provider.TryGet(bbgCode, "BBG", out var fs))
                            {
                                var monthYear = bbg.Substring(2, 2);
                                var qCode = fs.CodeConversions["BBG"].Where(x => x.Value == bbgCode.ToUpper()).First().Key;
                                var ibCode = fs.CodeConversions["IB"][qCode];
                                return $"{ibCode}{monthYear}";
                            }
                        }
                        else if (bbg.Length == 12 && bbg.Substring(5, 1) == " ") // ccMY Comdty format
                        {
                            var bbgCode = bbg.Left(3);
                            if (provider.TryGet(bbgCode, "BBG", out var fs))
                            {
                                var monthYear = bbg.Substring(3, 2);
                                var qCode = fs.CodeConversions["BBG"].Where(x => x.Value == bbgCode.ToUpper()).First().Key;
                                var ibCode = fs.CodeConversions["IB"][qCode];
                                return $"{ibCode}{monthYear}";
                            }
                        }
                    }
                    else //its an option
                    {

                    }
                    break;
                case "curncy":
                    break;
                case "equity":
                    break;
                case " index":
                    break;

            }

            return null;
        }

        public static string Right(this string str, int n)
        {
            return str.Substring(str.Length - n, n);
        }

        public static string Left(this string str, int n)
        {
            return str.Substring(0, n);
        }
    }
}
