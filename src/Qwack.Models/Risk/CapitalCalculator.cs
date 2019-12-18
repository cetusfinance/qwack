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
        public static double PVCapital(DateTime originDate, DateTime[] EADDates, double[] EADs, HazzardCurve hazzardCurve, IIrCurve discountCurve, double LGD)
        {
            if (EADDates.Length != EADs.Length)
                throw new Exception("Number of EPE dates and EPE values must be equal");
            
            var lastDate = originDate;
            var lastEad = EADs[0];
            var capital = 0.0;

            for(var i=0;i< EADDates.Length;i++)
            {
                if (EADDates[i] < originDate)
                    continue;

                var exposure = (lastEad + EADs[i]) / 2.0;
                var pDefault = hazzardCurve.GetDefaultProbability(lastDate, EADDates[i]);
                var df = discountCurve.GetDf(originDate, EADDates[i]);
                capital += exposure * pDefault * df * LGD;

                lastDate = EADDates[i];
                lastEad = EADs[i];
            }
            
            return capital;
        }

        public static double PVCapital(DateTime originDate, ICube ExpectedEAD, HazzardCurve hazzardCurve, IIrCurve discountCurve, double LGD)
        {
            (var eadDates, var eadValues) = XVACalculator.CubeToExposures(ExpectedEAD);
            return PVCapital(originDate, eadDates, eadValues, hazzardCurve, discountCurve, LGD);
        }

    }
}
