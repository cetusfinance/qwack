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
using static System.Math;

namespace Qwack.Models.Risk
{
    public class CapitalCalculator
    {
        public static double PVCapital_BII_IMM(DateTime originDate, DateTime[] EADDates, double[] EADs, HazzardCurve hazzardCurve, IIrCurve discountCurve, double LGD, Portfolio portfolio)
        {
            if (EADDates.Length != EADs.Length)
                throw new Exception("Number of EPE dates and EPE values must be equal");
            
            var pd = hazzardCurve.ConstantPD;
            var epees = EADs.Select((d,ix) => EADs.Skip(ix).Max()).ToArray();
            var Ms = EADDates.Select(d => portfolio.WeightedMaturity(d)).ToArray();
            var ks = epees.Select((e, ix) => BaselHelper.K(pd, LGD, Ms[ix]) * e *1.4).ToArray();

            var pvCapital = PvProfile(originDate, EADDates, ks, discountCurve);
            return pvCapital;
        }

        public static double PvCcrCapital_BII_SM(DateTime originDate, DateTime[] EADDates, IAssetFxModel[] models, Portfolio portfolio, HazzardCurve hazzardCurve, Currency reportingCurrency, IIrCurve discountCurve, double LGD, double[] eadProfile = null)
        {
            var pd = hazzardCurve.ConstantPD;
            var eads = eadProfile ?? EAD_BII_SM(originDate, EADDates, models, portfolio, reportingCurrency);

            var Ms = EADDates.Select(d => portfolio.WeightedMaturity(d)).ToArray();
            var ks = eads.Select((e, ix) => BaselHelper.K(pd, LGD, Ms[ix]) * e).ToArray();

            var pvCapital = PvProfile(originDate, EADDates, ks, discountCurve);
            return pvCapital;
        }

        public static double PvCvaCapital_BII_SM(DateTime originDate, DateTime[] EADDates, IAssetFxModel[] models, Portfolio portfolio, Currency reportingCurrency, IIrCurve discountCurve, double partyWeight, double[] eadProfile=null)
        {
            var eads = eadProfile??EAD_BII_SM(originDate, EADDates, models, portfolio, reportingCurrency);
            var Ms = EADDates.Select(d => portfolio.WeightedMaturity(d)).ToArray();
            var ks = eads.Select((e, ix) => XVACalculator.Capital_BaselII_CVA_SM(e, Ms[ix], partyWeight)).ToArray();

            var pvCapital = PvProfile(originDate, EADDates, ks, discountCurve);

            return pvCapital;
        }

        public static double[] EAD_BII_SM(DateTime originDate, DateTime[] EADDates, IAssetFxModel[] models, Portfolio portfolio, Currency reportingCurrency)
        {
            var ccf = 0.18;
            var eads = new double[EADDates.Length];

            for (var i = 0; i < EADDates.Length; i++)
            {
                if (EADDates[i] < originDate)
                    continue;
                models[i].AttachPortfolio(portfolio);
                var pv = portfolio.PV(models[i], reportingCurrency).SumOfAllRows;
                var delta = models[i].AssetCashDelta().SumOfAllRows;
                delta = Abs(delta); //need to consider buckets

                eads[i] = Max(pv, delta * ccf) * 1.4;
            }

            return eads;
        }

        public static double PVCapital_BII_IMM(DateTime originDate, ICube expectedEAD, HazzardCurve hazzardCurve, IIrCurve discountCurve, double LGD, Portfolio portfolio)
        {
            (var eadDates, var eadValues) = XVACalculator.CubeToExposures(expectedEAD);
            return PVCapital_BII_IMM(originDate, eadDates, eadValues, hazzardCurve, discountCurve, LGD, portfolio);
        }

        public static double PvProfile(DateTime originDate, DateTime[] exposureDates, double[] exposures , IIrCurve discountCurve)
        {
            var capital = 0.0;
            var time = 0.0;

            if (exposureDates.Length != exposures.Length || exposures.Length < 1)
                throw new DataMisalignedException();

            if (exposureDates.Length == 1)
                return discountCurve.GetDf(originDate, exposureDates[0]) * exposures[0];

            for (var i = 0; i < exposureDates.Length - 1; i++)
            {
                var exposure = (exposures[i] + exposures[i + 1]) / 2.0;
                var df = discountCurve.GetDf(originDate, exposureDates[i+1]);
                var dt = (exposureDates[i+1] - exposureDates[i]).TotalDays / 365.0;
                
                capital += exposure * dt * df;
                time += dt;
            }

            return capital / time;
        }

    }
}
