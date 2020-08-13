using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Cubes;
using Qwack.Core.Curves;
using Qwack.Core.Instruments;
using Qwack.Core.Instruments.Asset;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Math;
using Qwack.Math.Extensions;
using Qwack.Math.Interpolation;
using Qwack.Models.Models;
using Qwack.Transport.BasicTypes;
using static System.Math;
using static Qwack.Core.Basic.Consts.Cubes;

namespace Qwack.Models.Risk
{
    public class XVACalculator
    {
        public static double CVA(DateTime originDate, DateTime[] EPEDates, double[] EPEExposures, HazzardCurve hazzardCurve, IIrCurve discountCurve, double LGD)
        {
            if (EPEDates.Length != EPEExposures.Length)
                throw new Exception("Number of EPE dates and EPE values must be equal");
            
            var lastDate = originDate;
            var cva = 0.0;

            for(var i=0;i<EPEDates.Length;i++)
            {
                if (EPEDates[i] < originDate)
                    continue;

                var pDefault = hazzardCurve.GetDefaultProbability(lastDate, EPEDates[i]);
                var df = discountCurve.GetDf(originDate, EPEDates[i]);
                cva += EPEExposures[i] * pDefault * df * LGD;

                lastDate = EPEDates[i];
            }
            
            return -cva;
        }

        public static double CVA(DateTime originDate, ICube EPE, HazzardCurve hazzardCurve, IIrCurve discountCurve, double LGD)
        {
            (var epeDates, var epeValues) = CubeToExposures(EPE);
            return CVA(originDate, epeDates, epeValues, hazzardCurve, discountCurve, LGD);
        }

        public static double CVA_Approx(DateTime[] exposureDates, Portfolio portfolio, HazzardCurve hazzardCurve, IAssetFxModel model, IIrCurve discountCurve, double LGD, Currency reportingCurrency, ICurrencyProvider currencyProvider, Dictionary<DateTime,IAssetFxModel> models = null)
        {
            var exposures = EPE_Approx(exposureDates, portfolio, model, reportingCurrency, currencyProvider, models);
            return CVA(model.BuildDate, exposureDates, exposures, hazzardCurve, discountCurve, LGD);
        }

        public static double CVA_CapitalB3_Approx(DateTime[] exposureDates, Portfolio portfolio, double PD, IAssetFxModel model, double LGD, double partyWeight, Currency reportingCurrency, ICurrencyProvider currencyProvider, Dictionary<DateTime, IAssetFxModel> models = null)
        {
            var exposures = EPE_Approx(exposureDates, portfolio, model, reportingCurrency, currencyProvider, models);
            return RWA_BaselIII_CVA_IMM(model.BuildDate, exposureDates, exposures, PD, LGD, partyWeight)*0.11;
        }

        public static double[] EPE_Approx(DateTime[] exposureDates, Portfolio portfolio, IAssetFxModel model, Currency reportingCurrency, ICurrencyProvider currencyProvider, Dictionary<DateTime, IAssetFxModel> models = null)
        {
            var pfu = portfolio.UnWrapWrappers();
            var pf = pfu.UnStripStrips();

            //if (pf.AssetIds().Length != 1 || pf.FxPairs(model).Length != 0)
            //    throw new Exception("Portfolio can only contain a single asset and no FX indices for approximate CVA");

            if (!pf.Instruments.All(x => x is AsianSwap))
                throw new Exception("Approximate CVA only works for Asian Swap instruments");

            if (pf.Instruments
                .Select(x => Sign((x as AsianSwap).Notional))
                .Distinct()
                .Count() != 1)
                throw new Exception("All swaps must be in same direction");

            var cp = (pf.Instruments
                .Select(x => Sign((x as AsianSwap).Notional))
                .Distinct().First() == 1.0) ? OptionType.C : OptionType.P;

            var replicatingPF = new Portfolio
            {
                Instruments = pf.Instruments
                .Select(s => s as AsianSwap)
                .Select(s =>
               new AsianOption
               {
                   AssetId = s.AssetId,
                   AssetFixingId = s.AssetFixingId,
                   AverageStartDate = s.AverageStartDate,
                   AverageEndDate = s.AverageEndDate,
                   CallPut = cp,
                   Strike = s.Strike,
                   Counterparty = s.Counterparty,
                   DiscountCurve = s.DiscountCurve,
                   FixingCalendar = s.FixingCalendar,
                   FixingDates = s.FixingDates,
                   HedgingSet = s.HedgingSet,
                   Notional = Abs(s.Notional),
                   PaymentCalendar = s.PaymentCalendar,
                   PaymentCurrency = s.PaymentCurrency,
                   PaymentDate = s.PaymentDate,
                   PaymentLag = s.PaymentLag,
                   PaymentLagRollType = s.PaymentLagRollType,
                   SpotLag = s.SpotLag,
                   SpotLagRollType = s.SpotLagRollType,
                   PortfolioName = s.PortfolioName,
                   TradeId = s.TradeId ?? Guid.NewGuid().ToString(),
                   FxConversionType = s.FxConversionType,
                   FxFixingDates = s.FxFixingDates,
                   FxFixingId = s.FxFixingId
               } as IInstrument).ToList()
            };

            var exposures = new double[exposureDates.Length];
            var m = model.Clone();
            for (var i = 0; i < exposures.Length; i++)
            {
                if (models != null)
                    m = models[exposureDates[i]];
                else
                    m = m.RollModel(exposureDates[i], currencyProvider);

                var pvCube = replicatingPF.PV(m, reportingCurrency);
                foreach (var row in pvCube.GetAllRows())
                {
                    var tradeId = (string)row.MetaData[0];
                    var ins = replicatingPF.Instruments.Single(x => x.TradeId == tradeId) as AsianOption;
                    var df = m.FundingModel.GetCurve(ins.DiscountCurve).GetDf(m.BuildDate, ins.PaymentDate);
                    exposures[i] += row.Value / df;
                }
                exposures[i] = Max(0, exposures[i]);
            }

            return exposures;
        }

        public static (double FBA, double FCA) FVA(DateTime originDate, DateTime[] ExEDates, double[] EPEExposures, double[] ENEExposures, HazzardCurve hazzardCurve, IIrCurve discountCurve, IIrCurve fundingCurve)
        {
            if (ExEDates.Length != EPEExposures.Length)
                throw new Exception("Number of EPE dates and EPE values must be equal");

            if (ExEDates.Length != ENEExposures.Length)
                throw new Exception("Number of ENE dates and ENE values must be equal");


            var lastDate = originDate;
            var fba = 0.0;
            var fca = 0.0;
            for (var i = 0; i < ExEDates.Length; i++)
            {
                if (ExEDates[i] < originDate)
                    continue;

                var pSurvival = hazzardCurve.GetSurvivalProbability(lastDate, ExEDates[i]);
                var fwdDf = discountCurve.GetDf(lastDate, ExEDates[i]) / fundingCurve.GetDf(lastDate, ExEDates[i]) - 1.0;
                var df = fundingCurve.GetDf(originDate, ExEDates[i]);
                fba += -ENEExposures[i] * pSurvival * fwdDf * df;
                fca += -EPEExposures[i] * pSurvival * fwdDf * df;

                lastDate = ExEDates[i];
            }
            return (fba, fca);
        }

        public static (double FBA, double FCA) FVA(DateTime originDate, ICube EPE, ICube ENE, HazzardCurve hazzardCurve, IIrCurve discountCurve, IIrCurve fundingCurve)
        {
            if (!EPE.DataTypes.TryGetValue(ExposureDate, out var type) || type != typeof(DateTime))
                throw new Exception("EPE cube input not valid");

            if (!ENE.DataTypes.TryGetValue(ExposureDate, out var type2) || type2 != typeof(DateTime))
                throw new Exception("ENE cube input not valid");

            var rowsEPE = EPE.GetAllRows();
            var rowsENE = ENE.GetAllRows();
            if (rowsEPE.Length != rowsENE.Length)
                throw new Exception("EPE and ENE curves not of same size");

            var dateIx = EPE.GetColumnIndex(ExposureDate);
            var epeDates = new DateTime[rowsEPE.Length];
            var epeValues = new double[rowsEPE.Length];
            var eneValues = new double[rowsENE.Length];
            for (var i = 0; i < rowsENE.Length; i++)
            {
                epeDates[i] = (DateTime)rowsEPE[i].MetaData[dateIx];
                epeValues[i] = rowsEPE[i].Value;
                eneValues[i] = rowsENE[i].Value;
            }
            return FVA(originDate, epeDates, epeValues, eneValues, hazzardCurve, discountCurve, fundingCurve);
        }

        public static double KVA(DateTime originDate, DateTime[] ExEDates, double[] CapExposures, IIrCurve fundingCurve)
        {
            if (ExEDates.Length != CapExposures.Length)
                throw new Exception("Number of exposure dates and values must be equal");
            
            var lastDate = originDate;
            var kva = 0.0;
            for (var i = 0; i < ExEDates.Length; i++)
            {
                if (ExEDates[i] < originDate)
                    continue;

                var fwdDf = fundingCurve.GetDf(lastDate, ExEDates[i]);
                var df = fundingCurve.GetDf(originDate, ExEDates[i]);
                kva += CapExposures[i] / fwdDf * df;
                
                lastDate = ExEDates[i];
            }
            return kva;
        }

        public static double KVA(DateTime originDate, ICube ExpectedCapital, IIrCurve fundingCurve)
        {
            (var epeDates, var epeValues) = CubeToExposures(ExpectedCapital);
            return KVA(originDate, epeDates, epeValues, fundingCurve);
        }

        public static (double EEPE, double M) EEPE(DateTime originDate, DateTime[] exposureDates, double[] exposures)
        {
            if (exposureDates.Length != exposures.Length)
                throw new Exception("Exposures and dates must be of same length");

            var y1Date = originDate.AddYears(1);

            var eepe = 0.0;
            var sumDt = 0.0;
            var mTop = 0.0;
            var eee = new double[exposures.Length];
            for(var i=0;i<eee.Length;i++)
            {
                var dt = (i == 0) ? 0.0 : exposureDates[i - 1].CalculateYearFraction(exposureDates[i], DayCountBasis.Act365F);
                eee[i] = (i == 0) ? exposures[0] : Max(exposures[i], eee[i - 1]);
                if (exposureDates[i] <= y1Date)
                {
                    eepe += eee[i] * dt;
                    sumDt += dt;
                }
                else
                {
                    mTop += exposures[i] * dt;
                }
            }

            var M = Min(1.0, Max(5, 1.0 + mTop / eepe));
            eepe /= sumDt;

            return (eepe, M);
        }

        public static double RWA_BaselII_CCR_IMM(DateTime originDate, DateTime[] exposureDates, double[] exposures, double PD, double LGD, double alpha = 1.4)
        {
            (var efExpPE, var M) = EEPE(originDate, exposureDates, exposures);
            var ead = efExpPE * alpha;

            var RWA = BaselHelper.RWA(PD, LGD, M, ead);
            return RWA;
        }

        public static double RWA_BaselIII_CVA_IMM(DateTime originDate, DateTime[] exposureDates, double[] exposures, double PD, double LGD, double partyWeight, double alpha = 1.4)
        {
            (var efExpPE, var M) = EEPE(originDate, exposureDates, exposures);
            var ead = efExpPE * alpha;

            var RWA = 2.33 * partyWeight * M * ead; //simplified for single name and no hedges
            return RWA;
        }

        public static double Capital_BaselII_CVA_SM(double ead, double M, double partyWeight)
        {
            var RWA = 2.33 * partyWeight * M * ead; //simplified for single name and no hedges
            return RWA;
        }

        public static double RWA_BaselII_IMM(DateTime originDate, ICube EPE, double PD, double LGD, double alpha = 1.4)
        {
            (var epeDates, var epeValues) = CubeToExposures(EPE);
            return RWA_BaselII_CCR_IMM(originDate, epeDates, epeValues, PD, LGD, alpha);
        }

        public static double RWA_BaselIII_IMM(DateTime originDate, ICube EPE, double PD, double LGD, double partyWeight, double alpha = 1.4)
        {
            (var epeDates, var epeValues) = CubeToExposures(EPE);
            return RWA_BaselIII_CVA_IMM(originDate, epeDates, epeValues, PD, LGD, partyWeight, alpha);
        }

        public static (DateTime[] dates, double[] exposures) CubeToExposures(ICube EPE)
        {
            if (!EPE.DataTypes.TryGetValue(ExposureDate, out var type) || type != typeof(DateTime))
                throw new Exception("EPE cube input not valid");

            var rows = EPE.GetAllRows();
            var dateIx = EPE.GetColumnIndex(ExposureDate);
            var epeDates = new DateTime[rows.Length];
            var epeValues = new double[rows.Length];
            for (var i = 0; i < rows.Length; i++)
            {
                epeDates[i] = (DateTime)rows[i].MetaData[dateIx];
                epeValues[i] = rows[i].Value;
            }

            return (epeDates, epeValues);
        }
    }
}
