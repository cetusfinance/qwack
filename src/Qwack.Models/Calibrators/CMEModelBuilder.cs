using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Core.Instruments;
using Qwack.Core.Instruments.Funding;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Futures;
using Qwack.Paths.Processes;
using Qwack.Providers.CSV;
using Qwack.Transport.BasicTypes;
using Qwack.Transport.CmeXml;
using Calendar = Qwack.Dates.Calendar;
using static Qwack.Models.Calibrators.CMECommon;
using Qwack.Options.VolSurfaces;

namespace Qwack.Models.Calibrators
{
    public static class CMEModelBuilder
    {
        public static IrCurve GetCurveForCode(string cmeId, string cmeFilename, string qwackCode, string curveName, Dictionary<string,FloatRateIndex> indices, Dictionary<string,string> curves, IFutureSettingsProvider futureSettingsProvider, ICurrencyProvider currencyProvider, ICalendarProvider calendarProvider)
        {
            var parsed = CMEFileParser.Instance.Parse(cmeFilename).Where(r => r.ID == cmeId && r.SecTyp=="FUT");
            return GetCurveForCode(parsed, qwackCode, curveName, indices, curves, futureSettingsProvider, currencyProvider, calendarProvider);
        }

        public static IrCurve GetCurveForCode(string cmeId, StreamReader stream, string qwackCode, string curveName, Dictionary<string, FloatRateIndex> indices, Dictionary<string, string> curves, IFutureSettingsProvider futureSettingsProvider, ICurrencyProvider currencyProvider, ICalendarProvider calendarProvider)
        {
            var parsed = CMEFileParser.Instance.Parse(stream).Where(r => r.ID == cmeId && r.SecTyp == "FUT");
            return GetCurveForCode(parsed, qwackCode, curveName, indices, curves, futureSettingsProvider, currencyProvider, calendarProvider);
        }

        public static IrCurve GetCurveForCode(IEnumerable<CMEFileRecord> parsed, string qwackCode, string curveName, Dictionary<string, FloatRateIndex> indices, Dictionary<string, string> curves, IFutureSettingsProvider futureSettingsProvider, ICurrencyProvider currencyProvider, ICalendarProvider calendarProvider)
        {
            var q = parsed.ToDictionary(x => DateTime.ParseExact(x.MatDt, "yyyy-MM-dd", CultureInfo.InvariantCulture), x => x.SettlePrice);
            var origin = DateTime.ParseExact(parsed.First().BizDt, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            var instruments = parsed.Select(p => ToQwackIns(p, qwackCode, futureSettingsProvider, currencyProvider, indices, curves)).ToList();
            var pillars = instruments.Select(x => x.PillarDate).OrderBy(x => x).ToArray();
            var fic = new FundingInstrumentCollection(currencyProvider);
            fic.AddRange(instruments);
            var curve = new IrCurve(pillars, pillars.Select(p => 0.01).ToArray(), origin, curveName, Interpolator1DType.Linear, currencyProvider.GetCurrency("USD"));
            var fm = new FundingModel(origin, new[] { curve }, currencyProvider, calendarProvider);

            var solver = new NewtonRaphsonMultiCurveSolverStaged();
            solver.Solve(fm, fic);
            return curve;
        }

        public static BasicPriceCurve GetFuturesCurveForCode(string cmeSymbol, string cmeFutureFilename, ICurrencyProvider currency)
        {
            var parsed = CMEFileParser.Instance.Parse(cmeFutureFilename).Where(r => r.Sym == cmeSymbol && r.SecTyp=="FUT");
            var q = parsed.Where(x => x.SettlePrice.HasValue).ToDictionary(x => DateTime.ParseExact(x.MatDt, "yyyy-MM-dd", CultureInfo.InvariantCulture), x => x.SettlePrice);
            var datesVec = q.Keys.Distinct().OrderBy(x => x).ToArray();
            var labelsVec = parsed.Select(x => new Tuple<DateTime, string>(DateTime.ParseExact(x.MatDt, "yyyy-MM-dd", CultureInfo.InvariantCulture), x.MMY))
                .Distinct()
                .OrderBy(x => x.Item1)
                .Select(x => x.Item2)
                .ToArray();
            var pricesVec = datesVec.Select(l => System.Math.Max(q[l].Value, MinPrice)).ToArray();
            var origin = DateTime.ParseExact(parsed.First().BizDt, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            var curve = new BasicPriceCurve(origin, datesVec, pricesVec, PriceCurveType.NYMEX, currency, labelsVec)
            {
                SpotLag = 0.Bd()
            };
            return curve;
        }

        public static RiskyFlySurface GetFxSurfaceForCode(string cmeSymbol, string cmeFutureFilename, BasicPriceCurve priceCurve, ICurrencyProvider currency)
        {
            var parsed = CMEFileParser.Instance.Parse(cmeFutureFilename).Where(r => r.Sym == cmeSymbol && r.SecTyp=="OOF" && r.SettlePrice.HasValue);
            var (optionExerciseType, optionMarginingType) = OptionTypeFromCode(cmeSymbol);
            var origin = DateTime.ParseExact(parsed.First().BizDt, "yyyy-MM-dd", CultureInfo.InvariantCulture);

            var q = parsed.Where(x => x.SettlePrice > 0).Select(x => new ListedOptionSettlementRecord
            {
                CallPut = x.PutCall == 1 ? OptionType.C : OptionType.P,
                ExerciseType = optionExerciseType,
                MarginType = optionMarginingType,
                PV = x.SettlePrice.Value,
                Strike = x.StrkPx.Value,
                UnderlyingFuturesCode = x.UndlyMMY,
                ExpiryDate = DateTime.ParseExact(x.MatDt, "yyyy-MM-dd", CultureInfo.InvariantCulture),
                ValDate = origin
            }).Where(z => z.ExpiryDate > origin).ToList();

            var priceDict = priceCurve.PillarLabels.ToDictionary(x => x, x => priceCurve.GetPriceForDate(priceCurve.PillarDatesForLabel(x)));
            ListedSurfaceHelper.ImplyVols(q, priceDict, new ConstantRateIrCurve(0.0, origin, "dummy", currency.GetCurrency("USD")));
            var smiles = ListedSurfaceHelper.ToDeltaSmiles(q, priceDict);

            var expiries = smiles.Keys.OrderBy(x => x).ToArray();
            var ulByExpiry = expiries.ToDictionary(x => x, x => q.Where(qq => qq.ExpiryDate == x).First().UnderlyingFuturesCode);
            var fwdByExpiry = ulByExpiry.ToDictionary(x => x.Key, x => priceCurve.GetPriceForDate(priceCurve.PillarDatesForLabel(x.Value)));
            var fwds = expiries.Select(e => fwdByExpiry[e]).ToArray();
            var surface = ListedSurfaceHelper.ToRiskyFlySurface(smiles, origin, fwds, currency);

            return surface;
        }

        public static Dictionary<DateTime,double?> GetFuturesCurve(string cmeId, string cmeFilename)
        {
            var parsed = CMEFileParser.Instance.Parse(cmeFilename).Where(r => r.ID == cmeId && r.SecTyp == "FUT");
            var q = parsed.ToDictionary(x => DateTime.ParseExact(x.MatDt, "yyyy-MM-dd", CultureInfo.InvariantCulture), x => x.SettlePrice);
            return q;
        }

        public static IrCurve StripFxBasisCurve(string cmeFwdFileName, string ccyPair, Currency curveCcy, string curveName, DateTime valDate, IIrCurve baseCurve)
        {
            var fwds = GetFwdFxRatesFromFwdFile(cmeFwdFileName, ccyPair);
            var dfs = fwds.ToDictionary(f => f.Key, f =>  fwds[valDate] / f.Value * baseCurve.GetDf(valDate, f.Key));
            if (ccyPair.EndsWith("USD")) //flip dfs
            {
                dfs = dfs.ToDictionary(x => x.Key, x => 1.0 / x.Value);
            }
            var pillars = dfs.Keys.OrderBy(k => k).ToArray();
            var dfsValues = pillars.Select(p => dfs[p]).ToArray();
            var curve = new IrCurve(pillars, dfsValues, valDate, curveName, Interpolator1DType.Linear, curveCcy, null, RateType.DF);
            return curve;
        }

        public static IrCurve StripFxBasisCurve(string cmeFwdFileName, FxPair ccyPair, string cmePair, Currency curveCcy, string curveName, DateTime valDate, IrCurve baseCurve, ICurrencyProvider currencyProvider, ICalendarProvider calendarProvider)
        {
            var fwdsDict = GetFwdFxRatesFromFwdFile(cmeFwdFileName, new Dictionary<string, string> { { ccyPair.ToString(), cmePair } });
            var bc = baseCurve.Clone();
            bc.SolveStage = -1;
            var fwds = fwdsDict[ccyPair.ToString()];
            var spotDate = ccyPair.SpotDate(valDate);
            var spotRate = fwds[spotDate];
            fwds = Downsample(fwds, spotDate, ccyPair.PrimaryCalendar);

            var fwdObjects = fwds.Select(x => new FxForward
            {
                DomesticCCY = ccyPair.Domestic,
                DeliveryDate = x.Key,
                DomesticQuantity = 1e6,
                ForeignCCY = ccyPair.Foreign,
                PillarDate = x.Key,
                SolveCurve = curveName,
                Strike = x.Value,
                ForeignDiscountCurve = ccyPair.Foreign == curveCcy ? curveName : baseCurve.Name,
            });

            var fic = new FundingInstrumentCollection(currencyProvider);
            fic.AddRange(fwdObjects);
            var pillars = fwds.Keys.OrderBy(x => x).ToArray();
            var curve = new IrCurve(pillars, pillars.Select(p => 0.01).ToArray(), valDate, curveName, Interpolator1DType.Linear, curveCcy);
            var fm = new FundingModel(valDate, new[] { curve, bc }, currencyProvider, calendarProvider);
            var matrix = new FxMatrix(currencyProvider);
            var discoMap = new Dictionary<Currency, string> { { curveCcy, curveName }, { baseCurve.Currency, baseCurve.Name } };
            matrix.Init(ccyPair.Domestic, valDate, new Dictionary<Currency, double> { { ccyPair.Foreign, spotRate } }, new List<FxPair> { ccyPair }, discoMap);
            fm.SetupFx(matrix);
            var solver = new NewtonRaphsonMultiCurveSolverStaged() { InLineCurveGuessing = true };
            solver.Solve(fm, fic);

            return curve;
        }

        public static IrCurve StripFxBasisCurve(Dictionary<string, Dictionary<DateTime, double>>  fwdsDict, FxPair ccyPair, string cmePair, Currency curveCcy, string curveName, DateTime valDate, IrCurve baseCurve, ICurrencyProvider currencyProvider, ICalendarProvider calendarProvider)
        {
            var bc = baseCurve.Clone();
            bc.SolveStage = -1;
            var fwds = fwdsDict[ccyPair.ToString()];
            var spotDate = ccyPair.SpotDate(valDate);
            var spotRate = 0.0;
            while (!fwds.TryGetValue(spotDate, out spotRate))
            {
                spotDate = spotDate.AddDays(1);
            }

            fwds = Downsample(fwds, spotDate, ccyPair.PrimaryCalendar);

            var fwdObjects = fwds.Select(x => new FxForward
            {
                DomesticCCY = ccyPair.Domestic,
                DeliveryDate = x.Key,
                DomesticQuantity = 1e6,
                ForeignCCY = ccyPair.Foreign,
                PillarDate = x.Key,
                SolveCurve = curveName,
                Strike = x.Value,
                ForeignDiscountCurve = ccyPair.Foreign == curveCcy ? curveName : baseCurve.Name,
            });

            var fic = new FundingInstrumentCollection(currencyProvider);
            fic.AddRange(fwdObjects);
            var pillars = fwds.Keys.OrderBy(x => x).ToArray();
            var curve = new IrCurve(pillars, pillars.Select(p => 0.01).ToArray(), valDate, curveName, Interpolator1DType.Linear, curveCcy);
            var fm = new FundingModel(valDate, new[] { curve, bc }, currencyProvider, calendarProvider);
            var matrix = new FxMatrix(currencyProvider);
            var discoMap = new Dictionary<Currency, string> { { curveCcy, curveName }, { baseCurve.Currency, baseCurve.Name } };
            matrix.Init(ccyPair.Domestic, valDate, new Dictionary<Currency, double> { { ccyPair.Foreign, spotRate } }, new List<FxPair> { ccyPair }, discoMap);
            fm.SetupFx(matrix);
            var solver = new NewtonRaphsonMultiCurveSolverStaged() { InLineCurveGuessing = true };
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
                        PillarDate = edExp.AddMonths(3),
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

        private static Dictionary<string, string> _cmeCcyMap = new Dictionary<string, string>()
        {
            {"USDZAR","USDZRC" },
            {"USDJPY","USDJYC" },
            {"EURUSD","EURUSN" },
            {"GBPUSD","GBPUSN" },
            {"USDCAD","USDCAC" },
            {"AUDUSD","AUDUSN" },
            {"NZDUSD","NZDUSC" },
            //{"RUB","USDRUB" },
            //{"KRW","USDKRW" },
        };

        public static Dictionary<string, double> GetSpotFxRatesFromFwdFile(string filename, DateTime valDate, ICurrencyProvider currencyProvider, 
            ICalendarProvider calendarProvider) 
            => GetSpotFxRatesFromFwdFile(filename, valDate, _cmeCcyMap, currencyProvider, calendarProvider);

        public static Dictionary<string, double> GetSpotFxRatesFromFwdFile(string filename, DateTime valDate, Dictionary<string,string> pairMap, 
            ICurrencyProvider currencyProvider, ICalendarProvider calendarProvider)
        {
            var blob = CMEFileParser.Instance.GetBlob(filename);

            var supported = pairMap.Values.ToArray();
            var spotDates = pairMap.ToDictionary(x => x.Value, x => x.Key.FxPairFromString(currencyProvider, calendarProvider).SpotDate(valDate));
            var instruments = blob.Batch
                .Where(b => supported.Contains(b.Instrmt.Sym) && b.Instrmt.MatDt == spotDates[b.Instrmt.Sym]);

            var o = new Dictionary<string, double>();

            foreach (var kv in pairMap)
            {
                var ins = instruments.Where(i => i.Instrmt.Sym == kv.Value);
                if (ins.Count() > 1)
                    ins = new List<FIXMLMktDataFull>() { ins.First() };
                o.Add(kv.Key, Convert.ToDouble(ins.Single().Full.Single(f => f.Typ == "6").Px));
            }

            return o;
        }

        public static double GetSpotFxRateFromFwdFile(string filename, DateTime valDate, string ccyPair, ICurrencyProvider currencyProvider, ICalendarProvider calendarProvider) 
            => GetSpotFxRateFromFwdFile(filename, valDate, _cmeCcyMap[ccyPair], ccyPair, currencyProvider, calendarProvider);

        public static double GetSpotFxRateFromFwdFile(string filename, DateTime valDate, string cmeSymbol, string ccyPair, 
            ICurrencyProvider currencyProvider, ICalendarProvider calendarProvider)
        {
            var blob = CMEFileParser.Instance.GetBlob(filename);

            var pair = ccyPair.FxPairFromString(currencyProvider, calendarProvider);
            var spotDate = pair.SpotDate(valDate);
            var instruments = blob.Batch.Where(b => b.Instrmt.Sym == cmeSymbol && b.Instrmt.MatDt == spotDate);

            if (instruments.Count() > 1)
                throw new Exception();
            return Convert.ToDouble(instruments.Single().Full.Single(f => f.Typ == "6").Px);
        }

        public static Dictionary<DateTime,double> GetFwdFxRatesFromFwdFile(string filename, string ccyPair)
        {
            var fwds = GetFwdFxRatesFromFwdFile(filename,new Dictionary<string, string> { { ccyPair, _cmeCcyMap[ccyPair] } });
            return fwds[ccyPair];
        }

        public static Dictionary<string,Dictionary<DateTime, double>> GetFwdFxRatesFromFwdFile(string filename, Dictionary<string,string> ccyPairMap)
        {
            var blob = CMEFileParser.Instance.GetBlob(filename);
            return GetFwdFxRatesFromFwdFile(blob, ccyPairMap);
        }

        public static Dictionary<string, Dictionary<DateTime, double>> GetFwdFxRatesFromFwdFile(FIXML blob, Dictionary<string, string> ccyPairMap)
        {
            var o = new Dictionary<string, Dictionary<DateTime, double>>();
            foreach (var kv in ccyPairMap)
            {
                var cmeSymbol = kv.Value;
                var instruments = blob.Batch.Where(b => b.Instrmt.Sym == cmeSymbol);
                var dates = instruments.Select(x => x.Instrmt.MatDt).Distinct();
                var filteresIns = dates.Select(x => instruments.First(y => y.Instrmt.MatDt == x));
                if (!filteresIns.Any())
                    throw new Exception();
                var fwds = filteresIns.ToDictionary(x =>DateTime.ParseExact(x.Instrmt.MMY,"yyyyMMdd",CultureInfo.InvariantCulture), x => Convert.ToDouble(x.Full.Single(f => f.Typ == "6").Px));
                o.Add(kv.Key, fwds);
            }
            return o;
        }
    }
}
