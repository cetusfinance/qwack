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
    public class NewtonRaphsonAssetBasisCurveSolver
    {
        public double Tollerance { get; set; } = 0.00000001;
        public int MaxItterations { get; set; } = 1000;
        public int UsedItterations { get; set; }
        private const double JacobianBump = 0.0001;

        private DateTime _buildDate;
        private List<AsianBasisSwap> _curveInstruments;
        private DateTime[] _pillars;
        private IIrCurve _discountCurve;
        private IPriceCurve _currentCurve;
        private IPriceCurve _baseCurve;

        private SparsePriceCurveType _sparseType;
        private PriceCurveType _curveType;

        private int _numberOfInstruments;
        private int _numberOfPillars;
        private double[] _currentGuess;
        private double[] _currentPVs;
        private double[][] _jacobian;
        private ICurrencyProvider _currencyProvider;

        public SparsePriceCurve SolveSparseCurve(List<AsianBasisSwap> instruments, List<DateTime> pillars, 
            IIrCurve discountCurve, IPriceCurve baseCurve, DateTime buildDate, SparsePriceCurveType curveType,
            ICurrencyProvider currencyProvider)
        {
            _currencyProvider = currencyProvider;
            _curveInstruments = instruments;
            _pillars = pillars.ToArray();
            _numberOfInstruments = _curveInstruments.Count;
            _numberOfPillars = pillars.Count;
            _discountCurve = discountCurve;
            _buildDate = buildDate;
            _baseCurve = baseCurve;
            _sparseType = curveType;

            _currentGuess = Enumerable.Repeat(0.0, _numberOfPillars).ToArray();
            _currentCurve = new SparsePriceCurve(_buildDate, _pillars, _currentGuess, curveType, _currencyProvider);
            _currentPVs = ComputePVs();

            ComputeJacobianSparse();

            for (var i = 0; i < MaxItterations; i++)
            {
                ComputeNextGuess();
                _currentCurve = new SparsePriceCurve(_buildDate, _pillars, _currentGuess, curveType, _currencyProvider);

                _currentPVs = ComputePVs();
                if (_currentPVs.Max(x => System.Math.Abs(x)) < Tollerance)
                {
                    UsedItterations = i + 1;
                    break;
                }
                ComputeJacobianSparse();
            }

            return (SparsePriceCurve)_currentCurve;
        }

        public PriceCurve SolveCurve(List<AsianBasisSwap> instruments, List<DateTime> pillars, IIrCurve discountCurve, IPriceCurve baseCurve, DateTime buildDate, PriceCurveType curveType)
        {
            _curveInstruments = instruments;
            _pillars = pillars.ToArray();
            _numberOfInstruments = _curveInstruments.Count;
            _numberOfPillars = pillars.Count;
            _discountCurve = discountCurve;
            _buildDate = buildDate;
            _baseCurve = baseCurve;
            _curveType = curveType;

            _currentGuess = Enumerable.Repeat(0.0, _numberOfPillars).ToArray();
            _currentCurve = new PriceCurve(_buildDate, _pillars, _currentGuess, curveType);
            _currentPVs = ComputePVs();

            ComputeJacobian();

            for (var i = 0; i < MaxItterations; i++)
            {
                ComputeNextGuess();
                _currentCurve = new PriceCurve(_buildDate, _pillars, _currentGuess, curveType);

                _currentPVs = ComputePVs();
                if (_currentPVs.Max(x => System.Math.Abs(x)) < Tollerance)
                {
                    UsedItterations = i + 1;
                    break;
                }
                ComputeJacobian();
            }

            return (PriceCurve)_currentCurve;
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

        private void ComputeJacobianSparse()
        {
            _jacobian = Math.Matrix.DoubleArrayFunctions.MatrixCreate(_numberOfPillars, _numberOfPillars);

            for (var i = 0; i < _numberOfPillars; i++)
            {

                _currentCurve = new SparsePriceCurve(_buildDate, _pillars, _currentGuess.Select((g, ix) => ix == i ? g + JacobianBump : g).ToArray(), _sparseType, _currencyProvider);
                var bumpedPVs = ComputePVs();

                for (var j = 0; j < bumpedPVs.Length; j++)
                {
                    _jacobian[i][j] = (bumpedPVs[j] - _currentPVs[j]) / JacobianBump;
                }
            }
        }

        private void ComputeJacobian()
        {
            _jacobian = Math.Matrix.DoubleArrayFunctions.MatrixCreate(_numberOfPillars, _numberOfPillars);

            for (var i = 0; i < _numberOfPillars; i++)
            {

                _currentCurve = new PriceCurve(_buildDate, _pillars, _currentGuess.Select((g, ix) => ix == i ? g + JacobianBump : g).ToArray(), _curveType);
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
                o[i] = BasisSwapPv(_currentCurve, _curveInstruments[i], _discountCurve, _baseCurve);
            }
            return o;
        }

        public static double BasisSwapPv(IPriceCurve priceCurve, AsianBasisSwap swap, IIrCurve discountCurve, IPriceCurve baseCurve)
        {
            var baseIsPay = (swap.PaySwaplets.First().AssetId == baseCurve.AssetId);
            var payCurve = baseIsPay ? baseCurve : priceCurve;
            var recCurve = baseIsPay ? priceCurve : baseCurve;

            var payPVs = swap.PaySwaplets.Select(s =>
            (payCurve.GetAveragePriceForDates(s.FixingDates.AddPeriod(RollType.F, s.FixingCalendar, s.SpotLag)) - s.Strike)
            * (s.Direction == TradeDirection.Long ? 1.0 : -1.0)
            * s.Notional
            * discountCurve.GetDf(priceCurve.BuildDate, s.PaymentDate));

            var recPVs = swap.RecSwaplets.Select(s =>
            (recCurve.GetAveragePriceForDates(s.FixingDates.AddPeriod(RollType.F, s.FixingCalendar, s.SpotLag)) - s.Strike)
            * (s.Direction == TradeDirection.Long ? 1.0 : -1.0)
            * s.Notional
            * discountCurve.GetDf(priceCurve.BuildDate, s.PaymentDate));

            return payPVs.Sum() + recPVs.Sum();
        }
    }
}

