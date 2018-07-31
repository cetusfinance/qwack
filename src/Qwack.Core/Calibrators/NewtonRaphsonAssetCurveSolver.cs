using System;
using System.Collections.Generic;
using System.Linq;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Core.Instruments;
using Qwack.Core.Instruments.Asset;
using Qwack.Core.Instruments.Funding;
using Qwack.Core.Models;
using Qwack.Dates;

namespace Qwack.Core.Calibrators
{
    public class NewtonRaphsonAssetCurveSolver
    {
        public double Tollerance { get; set; } = 0.00000001;
        public int MaxItterations { get; set; } = 1000;
        public int UsedItterations { get; set; }
        private const double JacobianBump = 0.0001;

        private DateTime _buildDate;
        private FundingModel _curveEngine;
        private List<AsianSwapStrip> _curveInstruments;
        private DateTime[] _pillars;
        private ICurve _discountCurve;
        private IPriceCurve _currentCurve;

        private int _numberOfInstruments;
        private int _numberOfPillars;
        private double[] _currentGuess;
        private double[] _currentPVs;
        private double[][] _jacobian;
        private string[] _curveNames;

        public IPriceCurve Solve(List<AsianSwapStrip> instruments, List<DateTime> pillars, ICurve discountCurve, DateTime buildDate)
        {
            _curveInstruments = instruments;
            _pillars = pillars.ToArray();
            _numberOfInstruments = _curveInstruments.Count;
            _numberOfPillars = pillars.Count;
            _discountCurve = discountCurve;
            _buildDate = buildDate;

            _currentGuess = Enumerable.Repeat(instruments.Average(x => x.Swaplets.Average(s => s.Strike)), _numberOfPillars).ToArray();
            _currentCurve = new SparsePriceCurve(_buildDate, _pillars, _currentGuess, SparsePriceCurveType.Coal);
            _currentPVs = ComputePVs();

            ComputeJacobian();

            for (var i = 0; i < MaxItterations; i++)
            {
                ComputeNextGuess();
                _currentCurve = new SparsePriceCurve(_buildDate, _pillars, _currentGuess, SparsePriceCurveType.Coal);

                _currentPVs = ComputePVs();
                if (_currentPVs.Max(x => System.Math.Abs(x)) < Tollerance)
                {
                    UsedItterations = i + 1;
                    break;
                }
                ComputeJacobian();
            }

            return _currentCurve;
        }

        private void ComputeNextGuess()
        {
            var jacobianMi = Math.Matrix.DoubleArrayFunctions.InvertMatrix(_jacobian);
            var deltaGuess = Math.Matrix.DoubleArrayFunctions.MatrixProduct(_currentPVs, jacobianMi);
            for (var j = 0; j < _numberOfInstruments; j++)
            {
                _currentGuess[j] -= deltaGuess[j];
            }
        }

        private void ComputeJacobian()
        {
            _jacobian = Math.Matrix.DoubleArrayFunctions.MatrixCreate(_numberOfPillars, _numberOfPillars);

            for (var i = 0; i < _numberOfPillars; i++)
            {

                _currentCurve = new SparsePriceCurve(_buildDate, _pillars, _currentGuess.Select((g, ix) => ix == i ? g + JacobianBump : g).ToArray(), SparsePriceCurveType.Coal);
                var bumpedPVs = ComputePVs();

                for (var j = 0; j < bumpedPVs.Length; j++)
                {
                    _jacobian[i][j] = (bumpedPVs[j] - _currentPVs[j]) / JacobianBump;
                }
            }
        }

        private double[] ComputePVs()
        {
            var o = new double[_numberOfInstruments];
            for (var i = 0; i < o.Length; i++)
            {
                o[i] = SwapPv(_currentCurve,_curveInstruments[i],_discountCurve);
            }
            return o;
        }

        public static double SwapPv(IPriceCurve priceCurve, AsianSwapStrip swap, ICurve discountCurve)
        {
            var swapletPVs = swap.Swaplets.Select(s => 
            (priceCurve.GetAveragePriceForDates(s.FixingDates.AddPeriod(RollType.F, s.FixingCalendar, s.SpotLag))-s.Strike)
            *(s.Direction==TradeDirection.Long?1.0:-1.0)
            *discountCurve.GetDf(priceCurve.BuildDate,s.PaymentDate));

            return swapletPVs.Sum();
        }
    }
}

