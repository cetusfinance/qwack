using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Core.Calibrators;
using Qwack.Dates;
using Qwack.Futures;
using Qwack.Providers.CSV;
using Qwack.Core.Models;

namespace Qwack.Options.Calibrators
{
    public class CMEModelBuilder
    {
        public static IrCurve GetCurveForCode(string cmeId, string cmeFilename, string qwackCode, string curveName, IFutureSettingsProvider provider, ICurrencyProvider currencyProvider)
        {
            var parsed = CMEFileParser.Parse(cmeFilename).Where(r => r.ID == cmeId && r.SecType=="FUT");
            var q = parsed.ToDictionary(x => DateTime.ParseExact(x.MatDt, "MM/dd/yyyy", CultureInfo.InvariantCulture), x => x.SettlePrice);
            var origin = DateTime.ParseExact(parsed.First().BizDt, "MM/dd/yyyy", CultureInfo.InvariantCulture);
            var pillars = parsed.Select(x => DateTime.ParseExact(x.MatDt, "MM/dd/yyyy", CultureInfo.InvariantCulture)).ToArray();
            var curve = new IrCurve(pillars, pillars.Select(p => 0.01).ToArray(), origin, curveName, Math.Interpolation.Interpolator1DType.Linear, currencyProvider.GetCurrency("USD"));
            //var fm = new FundingModel()
            var solver = new NewtonRaphsonMultiCurveSolverStaged();
            //solver.Solve();
            return curve;
        }

        private string MmmYtoCode(string mmmY, string qwackCode)
        {
            var year = int.Parse(mmmY.Substring(0, 4));
            var month = int.Parse(mmmY.Substring(4, 2));

            if (year > DateTime.Today.Year + 8) //2-digit year
            {
                year -= 2000; 
                return $"{qwackCode}{DateExtensions.FutureMonths[month - 1]}{year:00}";
            }
            else
            {
                year = int.Parse(mmmY.Substring(3, 1));
                return $"{qwackCode}{DateExtensions.FutureMonths[month - 1]}{year:0}";
            }
        }
    }
}
