using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Math.Interpolation;
using Qwack.Dates;
using Qwack.Math;

namespace Qwack.Core.Curves
{
    public class HazzardCurve
    {
        public DateTime OriginDate { get; private set; }
        public DayCountBasis Basis { get; private set; }

        public double GetSurvivalProbability(DateTime startDate, DateTime endDate)
        {
            if (startDate == endDate) return 1.0;
            var pStart = GetSurvivalProbability(startDate);
            var pEnd = GetSurvivalProbability(endDate);
            return pEnd/pStart;
        }

        public double GetSurvivalProbability(DateTime endDate)
        {
            var t = Basis.CalculateYearFraction(OriginDate, endDate);
            var h = _hazzardCurve.Interpolate(t);
            var p = System.Math.Exp(-h * t);
            return p;
        }

        public double GetDefaultProbability(DateTime startDate, DateTime endDate) => 1.0 - GetDefaultProbability(startDate, endDate);

        private IInterpolator1D _hazzardCurve;

        public HazzardCurve(DateTime originDate, DayCountBasis basis, IInterpolator1D hazzardRateInterpolator)
        {
            OriginDate = originDate;
            Basis = basis;
            _hazzardCurve = hazzardRateInterpolator;
        }
    }
}
