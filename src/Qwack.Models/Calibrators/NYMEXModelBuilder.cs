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
using static Qwack.Models.Calibrators.CMECommon;
using System.Runtime.CompilerServices;

namespace Qwack.Models.Calibrators
{
    public class NYMEXModelBuilder
    {

        
        public static BasicPriceCurve GetCurveForCode(string nymexSymbol, string nymexFutureFilename, string qwackCode, IFutureSettingsProvider provider, ICurrencyProvider currency, PriceCurveType curveType)
        {
            var parsed = NYMEXFutureParser.Instance.Parse(nymexFutureFilename).Where(r=>r.Symbol==nymexSymbol);
            var q = parsed.Where(x=>x.Settle.HasValue).ToDictionary(x => Year2to1(x.Contract.Replace(nymexSymbol, qwackCode)), x => x.Settle);
            var datesDict = q.ToDictionary(x => FutureCode.GetExpiryFromCode(x.Key, provider), x => x.Key);
            var datesVec = datesDict.Keys.OrderBy(x => x).ToArray();
            var labelsVec = datesVec.Select(d => datesDict[d]).ToArray();
            var pricesVec = labelsVec.Select(l => System.Math.Max(q[l].Value,MinPrice)).ToArray();
            var origin = DateTime.ParseExact(parsed.First().TradeDate,"MM/dd/yyyy", CultureInfo.InvariantCulture);
            var curve = new BasicPriceCurve(origin, datesVec, pricesVec, curveType, currency, labelsVec)
            {
                AssetId = qwackCode,
                Name = qwackCode,
                SpotLag = 0.Bd()
            };
            return curve;
        }

        public static RiskyFlySurface GetSurfaceForCode(string nymexSymbol, string nymexOptionFilename, string qwackCode, BasicPriceCurve priceCurve, ICalendarProvider calendarProvider, ICurrencyProvider currency, IFutureSettingsProvider futureSettingsProvider)
        {
            var parsed = NYMEXOptionParser.Instance.Parse(nymexOptionFilename).Where(r => r.Symbol == nymexSymbol);
            var (optionExerciseType, optionMarginingType) = OptionTypeFromCode(nymexSymbol);
            var origin = DateTime.ParseExact(parsed.First().TradeDate, "MM/dd/yyyy", CultureInfo.InvariantCulture);

            var q = parsed.Where(x=>x.Settle>0).Select(x => new ListedOptionSettlementRecord
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
            
            var allOptionExpiries = new List<DateTime>();
            var lastDate = q.Max(x => x.ExpiryDate);

            var dummyFutureCode = $"{qwackCode}Z{DateExtensions.SingleDigitYear(DateTime.Today.Year + 2)}";
            var c = new FutureCode(dummyFutureCode, DateTime.Today.Year - 2, futureSettingsProvider);

            var contract = c.GetFrontMonth(origin, false);
            var lastContract = c.GetFrontMonth(lastDate, false);

            while(contract != lastContract)
            {
                var cc = new FutureCode(contract, origin.Year, futureSettingsProvider);
                var exp = ListedUtils.FuturesCodeToDateTime(contract);
                var record = new NYMEXOptionRecord
                {
                    ContractMonth = exp.Month,
                    ContractYear = exp.Year,
                    Symbol = nymexSymbol
                };
                var optExpiry = OptionExpiryFromNymexRecord(record, calendarProvider);
                if (optExpiry > origin)
                    allOptionExpiries.Add(optExpiry);

                contract = cc.GetNextCode(false);
            }

            var surface = ListedSurfaceHelper.ToRiskyFlySurfaceStepFlat(smiles, origin, priceCurve, allOptionExpiries, currency);

            return surface;
        }

       
    }
}
