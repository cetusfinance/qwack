using System;
using System.Collections.Generic;
using System.Linq;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Core.Instruments.Funding;
using Qwack.Dates;
using Qwack.Options.VolSurfaces;
using Qwack.Providers.CSV;
using Qwack.Transport.BasicTypes;
using static Qwack.Models.Calibrators.CMECommon;

namespace Qwack.Models.Calibrators
{
    public class COMEXModelBuilder
    {


        public static (IrCurve curve, double spotPrice) GetMetalCurveForCode(string cmxSettleFwdFilename, string cmxSymbol, FxPair metalPair, string curveName, DateTime valDate, IIrCurve baseCurve, ICurrencyProvider currencyProvider, ICalendarProvider calendarProvider)
        {
            var blob = CMEFileParser.Instance.GetBlob(cmxSettleFwdFilename);
            var fwds = blob.Batch.Where(b => b.Instrmt.Sym == cmxSymbol).ToDictionary(x => x.Instrmt.MatDt, x => Convert.ToDouble(x.Full.Where(x => x.Typ == "6").First().Px));

            var curveCcy = metalPair.Domestic;
            var bc = (baseCurve as IrCurve).Clone();
            bc.SolveStage = -1;
            var spotDate = metalPair.SpotDate(valDate);
            var spotRate = Convert.ToDouble(fwds[spotDate]);
            fwds = Downsample(fwds, spotDate, metalPair.PrimaryCalendar);

            var fwdObjects = fwds.Select(x => new FxForward
            {
                DomesticCCY = metalPair.Foreign,
                DeliveryDate = x.Key,
                DomesticQuantity = 1e6,
                ForeignCCY = metalPair.Domestic,
                PillarDate = x.Key,
                SolveCurve = curveName,
                Strike = Convert.ToDouble(x.Value),
                ForeignDiscountCurve = baseCurve.Name,
            });

            var fic = new FundingInstrumentCollection(currencyProvider, calendarProvider);
            fic.AddRange(fwdObjects);
            var pillars = fwds.Keys.OrderBy(x => x).ToArray();
            var curve = new IrCurve(pillars, pillars.Select(p => 0.01).ToArray(), valDate, curveName, Interpolator1DType.Linear, curveCcy);
            var fm = new FundingModel(valDate, new[] { curve, bc }, currencyProvider, calendarProvider);
            var matrix = new FxMatrix(currencyProvider);
            var discoMap = new Dictionary<Currency, string> { { curveCcy, curveName }, { (baseCurve as IrCurve).Currency, baseCurve.Name } };
            matrix.Init(metalPair.Foreign, valDate, new Dictionary<Currency, double> { { metalPair.Domestic, spotRate } }, new List<FxPair> { metalPair }, discoMap);
            fm.SetupFx(matrix);
            var solver = new NewtonRaphsonMultiCurveSolverStaged() { InLineCurveGuessing = true };
            solver.Solve(fm, fic);

            return (curve, spotRate);
        }

        public static RiskyFlySurface GetMetalSurfaceForCode(string cmxSymbol, string cmxSettleFilename, ICurrencyProvider currency)
        {
            var blob = CMEFileParser.Instance.GetBlob(cmxSettleFilename);
            var origin = blob.Batch.First().BizDt;
            var (optionExerciseType, optionMarginingType) = OptionTypeFromCode(cmxSymbol);
            var opts = blob.Batch.Where(b => b.Instrmt.Sym == cmxSymbol).Select(x => new ListedOptionSettlementRecord
            {
                Strike = Convert.ToDouble(x.Instrmt.StrkPx),
                PV = Convert.ToDouble(x.Full.Where(x => x.Typ == "6").First().Px),
                CallPut = x.Instrmt.PutCall ? OptionType.C : OptionType.P, //x.Instrmt.PutCall == 1 ? OptionType.C : OptionType.P,
                ExerciseType = optionExerciseType,
                MarginType = optionMarginingType,
                UnderlyingFuturesCode = $"{x.Undly.ID}~{x.Undly.MMY}",
                ExpiryDate = x.Instrmt.MatDt,
                ValDate = origin
            }).Where(z => z.ExpiryDate > origin).ToList();

            var underlyings = opts.Select(x => x.UnderlyingFuturesCode).Distinct().ToArray();
            var ulCodes = underlyings.Select(x => x.Split('~')[0]).Distinct().ToArray();

            var futRecords = blob.Batch.Where(b => b.Instrmt.SecTyp == "FUT" && ulCodes.Contains(b.Instrmt.ID));
            var priceDict = futRecords.ToDictionary(x => $"{x.Instrmt.ID}~{x.Instrmt.MMY}", x => Convert.ToDouble(x.Full.Where(x => x.Typ == "6").First().Px));
            ListedSurfaceHelper.ImplyVols(opts, priceDict, new ConstantRateIrCurve(0.0, origin, "dummy", currency.GetCurrency("USD")));
            var smiles = ListedSurfaceHelper.ToDeltaSmiles(opts, priceDict);

            var expiries = smiles.Keys.OrderBy(x => x).ToArray();
            var ulByExpiry = expiries.ToDictionary(x => x, x => opts.Where(qq => qq.ExpiryDate == x).First().UnderlyingFuturesCode);
            var fwdByExpiry = ulByExpiry.ToDictionary(x => x.Key, x => priceDict[x.Value]);
            var fwds = expiries.Select(e => fwdByExpiry[e]).ToArray();
            var surface = ListedSurfaceHelper.ToRiskyFlySurface(smiles, origin, fwds, currency);

            return surface;
        }


    }
}
