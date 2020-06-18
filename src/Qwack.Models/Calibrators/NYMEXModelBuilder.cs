using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Qwack.Core.Curves;
using Qwack.Providers.CSV;
using Qwack.Futures;
using Qwack.Core.Basic;
using Qwack.Dates;
using Qwack.Options.VolSurfaces;
using System.Globalization;
using Qwack.Transport.BasicTypes;

namespace Qwack.Models.Calibrators
{
    public class NYMEXModelBuilder
    {
        private static double MinPrice = 1e-10;
        
        public static BasicPriceCurve GetCurveForCode(string nymexSymbol, string nymexFutureFilename, string qwackCode, IFutureSettingsProvider provider, ICurrencyProvider currency)
        {
            var parsed = NYMEXFutureParser.Parse(nymexFutureFilename).Where(r=>r.Symbol==nymexSymbol);
            var q = parsed.ToDictionary(x => Year2to1(x.Contract.Replace(nymexSymbol, qwackCode)), x => x.Settle);
            var datesDict = q.ToDictionary(x => FutureCode.GetExpiryFromCode(x.Key, provider), x => x.Key);
            var datesVec = datesDict.Keys.OrderBy(x => x).ToArray();
            var labelsVec = datesVec.Select(d => datesDict[d]).ToArray();
            var pricesVec = labelsVec.Select(l => System.Math.Max(q[l].Value,MinPrice)).ToArray();
            var origin = DateTime.ParseExact(parsed.First().TradeDate,"MM/dd/yyyy", CultureInfo.InvariantCulture);
            var curve = new BasicPriceCurve(origin, datesVec, pricesVec, PriceCurveType.NYMEX, currency, labelsVec)
            {
                AssetId = qwackCode,
                Name = qwackCode,
                SpotLag = 0.Bd()
            };
            return curve;
        }

        public static RiskyFlySurface GetSurfaceForCode(string nymexSymbol, string nymexOptionFilename, string qwackCode, BasicPriceCurve priceCurve, ICalendarProvider calendarProvider, ICurrencyProvider currency)
        {
            var parsed = NYMEXOptionParser.Parse(nymexOptionFilename).Where(r => r.Symbol == nymexSymbol);
            var (optionExerciseType, optionMarginingType) = OptionTypeFromCode(nymexSymbol);
            var origin = DateTime.ParseExact(parsed.First().TradeDate, "MM/dd/yyyy", CultureInfo.InvariantCulture);

            var q = parsed.Select(x => new ListedOptionSettlementRecord
            {
                CallPut = x.PutCall=="C"?OptionType.C:OptionType.P,
                ExerciseType = optionExerciseType,
                MarginType = optionMarginingType,
                PV = x.Settle,
                Strike = x.Strike,
                UnderlyingFuturesCode = Year2to1(x.Contract.Split(' ')[0].Replace(nymexSymbol, qwackCode)),
                ExpiryDate = OptionExpiryFromNymexRecord(x, calendarProvider),
                ValDate = origin
            }).Where(z=>z.ExpiryDate> origin).ToList();

            var priceDict = priceCurve.PillarLabels.ToDictionary(x => x, x => priceCurve.GetPriceForDate(priceCurve.PillarDatesForLabel(x)));
            ListedSurfaceHelper.ImplyVols(q, priceDict, new ConstantRateIrCurve(0.0, origin, "dummy", currency.GetCurrency("USD")));
            var smiles = ListedSurfaceHelper.ToDeltaSmiles(q, priceDict);
            var surface = ListedSurfaceHelper.ToRiskyFlySurface(smiles, origin, smiles.Keys.Select(x => priceCurve.GetPriceForDate(x)).ToArray());

            return surface;
        }

        private static string Year2to1(string input)
        {
            if (int.Parse(input.Substring(input.Length - 2)) > DateTime.Today.Year - 2000 + 8)
                return input;
            else
                return input.Substring(0, input.Length - 2) + input.Substring(input.Length - 1);
        }
        
        private static (OptionExerciseType optionExerciseType, OptionMarginingType optionMarginingType) OptionTypeFromCode(string code)
        {
            switch(code)
            {
                case "CL":
                case "LO":
                case "PO":
                case "PAO":
                case "ON":
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

        private static DateTime OptionExpiryFromNymexRecord(NYMEXOptionRecord record, ICalendarProvider calendarProvider)
        {
            switch(record.Symbol)
            {
                case "LO":
                    return new DateTime(record.ContractYear, record.ContractMonth, 26)
                        .AddMonths(-1)
                        .SubtractPeriod(RollType.P, calendarProvider.Collection["NYC"], 7.Bd());
                case "BZO":
                    return new DateTime(record.ContractYear, record.ContractMonth, 1)
                        .AddMonths(-2)
                        .SubtractPeriod(RollType.P, calendarProvider.Collection["LON"], 4.Bd());
                default:
                    throw new Exception($"No option expiry mapping found for {record.Symbol}");
            }
        }
    }
}
