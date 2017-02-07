using Qwack.Core.Basic;
using Qwack.Dates;
using Qwack.Math.Interpolation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Qwack.Core.VolSurfaces
{
    /// <summary>
    /// A volatility which returns a value interpolated from a grid of vols for varying strikes and maturities
    /// Strikes can be either absolute or delta type
    /// Interpolation method for strike and time dimensions can be specified seperately
    /// </summary>
    public class GridVolSurface : IVolSurface
    {
        public DateTime OriginDate { get; set; }
        public double[] Strikes { get; set; }
        public StrikeType StrikeType { get; set; }
        public Interpolator1DType StrikeInterpolatorType { get; set; } = Interpolator1DType.LinearFlatExtrap;
        public double[] ExpiriesDouble { get; set; }
        public Interpolator1DType TimeInterpolatorType { get; set; } = Interpolator1DType.LinearInVariance;
        public double[][] Volatilities { get; set; }
        public DateTime[] Expiries { get; set; }
        public DayCountBasis TimeBasis { get; set; } = DayCountBasis.Act365F;

        private IInterpolator1D[] _interpolators;

        public GridVolSurface()         {        }

        public GridVolSurface(DateTime originDate, double[] strikes, DateTime[] expiries, double[][] vols)
        {
            Build(originDate, strikes, expiries, vols);
        }

        public GridVolSurface(DateTime originDate, double[] strikes, DateTime[] expiries, double[][] vols, 
            StrikeType strikeType, Interpolator1DType strikeInterpType, Interpolator1DType timeInterpType, 
            DayCountBasis timeBasis)
        {
            StrikeType = strikeType;
            StrikeInterpolatorType = strikeInterpType;
            TimeInterpolatorType = timeInterpType;
            TimeBasis = timeBasis;

            Build(originDate, strikes, expiries, vols);
        }

        public void Build(DateTime originDate, double[] strikes, DateTime[] expiries, double[][] vols)
        {
            OriginDate = originDate;
            Strikes = strikes;
            Expiries = expiries;
            ExpiriesDouble = Expiries.Select(t => TimeBasis.CalculateYearFraction(originDate, t)).ToArray();
            _interpolators = vols.Select((v, ix) => 
                InterpolatorFactory.GetInterpolator(Strikes, v, StrikeInterpolatorType)).ToArray();
        }

        public double GetVolForAbsoluteStrike(double strike, double maturity)
        {
            var ix = FindFloorPoint(maturity);

            return 1.0;
        }

        public double GetVolForAbsoluteStrike(double strike, DateTime expiry)
        {
            return 1.0;
        }

        public double GetVolForDeltaStrike(double deltaStrike, double maturity)
        {
            return 1.0;
        }

        public double GetVolForDeltaStrike(double strike, DateTime expiry)
        {
            return 1.0;
        }

        private int FindFloorPoint(double t)
        {
            int index = Array.BinarySearch(ExpiriesDouble, t);
            if (index < 0)
            {
                index = ~index - 1;
            }

            return System.Math.Min(System.Math.Max(index, 0), ExpiriesDouble.Length - 2);
        }
    }
}
