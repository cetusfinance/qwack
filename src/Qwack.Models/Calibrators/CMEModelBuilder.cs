using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Core.Instruments;
using Qwack.Core.Instruments.Funding;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Futures;
using Qwack.Providers.CSV;

namespace Qwack.Models.Calibrators
{
    public static class CMEModelBuilder
    {
        public static IrCurve GetCurveForCode(string cmeId, string cmeFilename, string qwackCode, string curveName, Dictionary<string,FloatRateIndex> indices, Dictionary<string,string> curves, IFutureSettingsProvider futureSettingsProvider, ICurrencyProvider currencyProvider, ICalendarProvider calendarProvider)
        {
            var parsed = CMEFileParser.Parse(cmeFilename).Where(r => r.ID == cmeId && r.SecTyp=="FUT");
            var q = parsed.ToDictionary(x => DateTime.ParseExact(x.MatDt, "yyyy-MM-dd", CultureInfo.InvariantCulture), x => x.SettlePrice);
            var origin = DateTime.ParseExact(parsed.First().BizDt, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            var instruments = parsed.Select(p => ToQwackIns(p, qwackCode, futureSettingsProvider, currencyProvider, indices, curves)).ToList();
            var pillars = instruments.Select(x => x.PillarDate).OrderBy(x => x).ToArray();
            var fic = new FundingInstrumentCollection(currencyProvider);
            fic.AddRange(instruments);
            var curve = new IrCurve(pillars, pillars.Select(p => 0.01).ToArray(), origin, curveName, Math.Interpolation.Interpolator1DType.Linear, currencyProvider.GetCurrency("USD"));
            var fm = new FundingModel(origin, new[] { curve }, currencyProvider, calendarProvider);

            var solver = new NewtonRaphsonMultiCurveSolverStaged();
            solver.Solve(fm, fic);
            return curve;
        }
        
        private static IFundingInstrument ToQwackIns(this CMEFileRecord record, string qwackCode, IFutureSettingsProvider futureSettingsProvider, ICurrencyProvider currencyProvider, Dictionary<string, FloatRateIndex> indices,Dictionary<string,string> forecastCurves)
        {
            switch(qwackCode)
            {
                case "ED":
                    var edExp = FutureCode.GetExpiryFromCode(MmmYtoCode(record.MMY, qwackCode), futureSettingsProvider);
                    return new STIRFuture
                    {
                        ContractSize=1e6,
                        Currency=currencyProvider.GetCurrency("USD"),
                        DCF=0.25,
                        ConvexityAdjustment = 0,
                        Price = record.SettlePrice.Value,
                        Index = indices["ED"],
                        Expiry = edExp,
                        PillarDate = edExp,
                        TradeId = qwackCode+record.MMY,
                        Position=1,
                        SolveCurve = forecastCurves["ED"],
                        ForecastCurve = forecastCurves["ED"],
                    };
                case "FF":
                    var ffEnd = FutureCode.GetExpiryFromCode(MmmYtoCode(record.MMY, qwackCode), futureSettingsProvider);
                    var ffStart = ffEnd.FirstDayOfMonth();
                    return new OISFuture
                    {
                        ContractSize = 1e6,
                        Currency = currencyProvider.GetCurrency("USD"),
                        DCF = ffStart.CalculateYearFraction(ffEnd,DayCountBasis.ACT360),
                        Price = record.SettlePrice.Value,
                        Index = indices["FF"],
                        PillarDate = ffEnd,
                        TradeId = qwackCode + record.MMY,
                        Position = 1,
                        SolveCurve = forecastCurves["FF"],
                        ForecastCurve = forecastCurves["FF"],
                        AverageStartDate = ffStart,
                        AverageEndDate = ffEnd,
                    };
                default:
                    throw new Exception($"No mapping found for code {qwackCode}");
            }
        }

        private static string MmmYtoCode(string mmmY, string qwackCode)
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
