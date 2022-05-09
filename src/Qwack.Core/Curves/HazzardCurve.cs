using System;
using Qwack.Dates;
using Qwack.Math;
using Qwack.Math.Interpolation;
using Qwack.Transport.BasicTypes;
using Qwack.Transport.TransportObjects.MarketData.Curves;

namespace Qwack.Core.Curves
{
    public class HazzardCurve
    {
        private readonly IInterpolator1D _hazzardCurve;
        private double? _constPD = null;

        public HazzardCurve(DateTime originDate, DayCountBasis basis, IInterpolator1D hazzardRateInterpolator)
        {
            OriginDate = originDate;
            Basis = basis;
            _hazzardCurve = hazzardRateInterpolator;
        }

        public HazzardCurve(TO_HazzardCurve transportObject) : this(transportObject.OriginDate, transportObject.Basis, InterpolatorFactory.GetInterpolator(transportObject.HazzardCurve))
        {
            if (transportObject.ConstantPD.HasValue)
                ConstantPD = transportObject.ConstantPD.Value;
        }

        public double ConstantPD
        {
            get
            {
                if (_constPD.HasValue)
                    return _constPD.Value;
                else
                    return GetDefaultProbability(OriginDate, OriginDate.AddDays(365));
            }
            set => _constPD = value;
        }

        public DateTime OriginDate { get; private set; }
        public DayCountBasis Basis { get; private set; }

        public double GetSurvivalProbability(DateTime startDate, DateTime endDate)
        {
            if (startDate == endDate) return 1.0;
            var pStart = GetSurvivalProbability(startDate);
            var pEnd = GetSurvivalProbability(endDate);
            return pEnd / pStart;
        }

        public double GetSurvivalProbability(DateTime endDate)
        {
            var t = Basis.CalculateYearFraction(OriginDate, endDate);
            var p = _hazzardCurve.Interpolate(t);
            return p;
        }

        public double GetSurvivalProbabilitySlope(DateTime endDate)
        {
            var t = Basis.CalculateYearFraction(OriginDate, endDate);
            var p = _hazzardCurve.FirstDerivative(t);
            return p;
        }

        public double GetDefaultProbability(DateTime startDate, DateTime endDate) => 1.0 - GetSurvivalProbability(startDate, endDate);

        public double RiskyDiscountFactor(DateTime startDate, DateTime endDate, IIrCurve discountCurve, double LGD)
        {
            var dp = GetDefaultProbability(startDate, endDate);
            var el = dp * LGD;
            var df = discountCurve.GetDf(startDate, endDate);
            return (1.0 - el) * df;
        }
    }
}
