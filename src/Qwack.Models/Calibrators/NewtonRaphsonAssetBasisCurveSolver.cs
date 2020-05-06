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
using Qwack.Transport.BasicTypes;
using Qwack.Utils.Parallel;

namespace Qwack.Models.Calibrators
{
    public class NewtonRaphsonAssetBasisCurveSolver : INewtonRaphsonAssetBasisCurveSolver
    {
        public double Tollerance { get; set; } = 0.00000001;
        public int MaxItterations { get; set; } = 1000;
        public int UsedItterations { get; set; }
        private const double JacobianBump = 0.0001;

        private DateTime _buildDate;
        private List<IAssetInstrument> _curveInstruments;
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

        public NewtonRaphsonAssetBasisCurveSolver(ICurrencyProvider currencyProvider) => _currencyProvider = currencyProvider;

        public SparsePriceCurve SolveSparseCurve(List<IAssetInstrument> instruments, List<DateTime> pillars,
            IIrCurve discountCurve, IPriceCurve baseCurve, DateTime buildDate, SparsePriceCurveType curveType)
        {
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
            _currentPVs = ComputePVs(_currentCurve);

            ComputeJacobianSparse();

            for (var i = 0; i < MaxItterations; i++)
            {
                ComputeNextGuess();
                _currentCurve = new SparsePriceCurve(_buildDate, _pillars, _currentGuess, curveType, _currencyProvider);

                _currentPVs = ComputePVs(_currentCurve);
                if (_currentPVs.Max(x => System.Math.Abs(x)) < Tollerance)
                {
                    UsedItterations = i + 1;
                    break;
                }
                ComputeJacobianSparse();
            }

            return (SparsePriceCurve)_currentCurve;
        }

        public PriceCurve SolveCurve(IEnumerable<IAssetInstrument> instruments, List<DateTime> pillars, IIrCurve discountCurve, IPriceCurve baseCurve, DateTime buildDate, PriceCurveType curveType)
        {
            _curveInstruments = instruments.OrderBy(x => x.LastSensitivityDate).ToList();
            _pillars = pillars.ToArray();
            _numberOfInstruments = _curveInstruments.Count;
            _numberOfPillars = pillars.Count;
            _discountCurve = discountCurve;
            _buildDate = buildDate;
            _baseCurve = baseCurve;
            _curveType = curveType;

            _currentGuess = _curveInstruments.Select(x => InitialGuess(x)).ToArray();
            //Enumerable.Repeat(0.0, _numberOfPillars).ToArray();
            _currentCurve = new PriceCurve(_buildDate, _pillars, _currentGuess, curveType, _currencyProvider);
            _currentPVs = ComputePVs(_currentCurve);

            ComputeJacobian();

            for (var i = 0; i < MaxItterations; i++)
            {
                ComputeNextGuess();
                if (_currentGuess.Any(x => double.IsNaN(x)))
                    throw new Exception($"NaNs detected in solution at step {i}");

                _currentCurve = new PriceCurve(_buildDate, _pillars, _currentGuess, curveType, _currencyProvider);

                _currentPVs = ComputePVs(_currentCurve);
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
                var bumpedPVs = ComputePVs(_currentCurve);

                for (var j = 0; j < bumpedPVs.Length; j++)
                {
                    _jacobian[i][j] = (bumpedPVs[j] - _currentPVs[j]) / JacobianBump;
                }
            }
        }

        private void ComputeJacobian()
        {
            _jacobian = Math.Matrix.DoubleArrayFunctions.MatrixCreate(_numberOfPillars, _numberOfPillars);

            ParallelUtils.Instance.For(0, _numberOfPillars, 1, i =>
            //for (var i = 0; i < _numberOfPillars; i++)
            {
                var currentCurve = new PriceCurve(_buildDate, _pillars, _currentGuess.Select((g, ix) => ix == i ? g + JacobianBump : g).ToArray(), _curveType, _currencyProvider);
                var bumpedPVs = ComputePVs(currentCurve);

                for (var j = 0; j < bumpedPVs.Length; j++)
                {
                    _jacobian[i][j] = (bumpedPVs[j] - _currentPVs[j]) / JacobianBump;
                }
            }, false).Wait();
        }

        private double[] ComputePVs(IPriceCurve priceCurve)
        {
            var o = new double[_numberOfInstruments];
            for (var i = 0; i < o.Length; i++)
            {
                o[i] = BasisSwapPv(priceCurve, _curveInstruments[i], _discountCurve, _baseCurve);
            }
            return o;
        }

        public static double BasisSwapPv(IPriceCurve priceCurve, IAssetInstrument instrument, IIrCurve discountCurve, IPriceCurve baseCurve)
        {
            switch (instrument)
            {
                case AsianBasisSwap swap:
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
                case Future fut:
                    var fwd = priceCurve.GetPriceForDate(fut.ExpiryDate);
                    return (fwd - fut.Strike) * fut.ContractQuantity * fut.LotSize;
                default:
                    throw new Exception("Unable to process instrument type in solver");
            }
        }

        private double InitialGuess(IAssetInstrument instrument)
        {
            switch (instrument)
            {
                case AsianBasisSwap swap:
                    var baseIsPay = (swap.PaySwaplets.First().AssetId == _baseCurve.AssetId);
                    return baseIsPay
                        ? _baseCurve.GetPriceForDate(swap.RecSwaplets.Last().LastSensitivityDate)
                        : _baseCurve.GetPriceForDate(swap.PaySwaplets.Last().LastSensitivityDate);
                case Future fut:
                    return _baseCurve.GetPriceForDate(fut.ExpiryDate);
                default:
                    throw new Exception("Unable to process instrument type in solver");
            }
        }
    }
}

