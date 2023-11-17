using System;
using System.Collections.Generic;
using System.Linq;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Core.Instruments;
using Qwack.Core.Instruments.Asset;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Models.Models;
using Qwack.Transport.BasicTypes;

namespace Qwack.Models.Calibrators
{
    public class NewtonRaphsonAssetCurveSolver
    {
        public double Tollerance { get; set; } = 0.00000001;
        public int MaxItterations { get; set; } = 1000;
        public int UsedItterations { get; set; }
        private const double JacobianBump = 0.0001;

        private DateTime _buildDate;
        private List<AsianSwapStrip> _curveInstruments;
        private List<IAssetInstrument> _curveGenericInstruments;

        private DateTime[] _pillars;
        private IIrCurve _discountCurve;
        private IPriceCurve _currentCurve;

        private int _numberOfInstruments;
        private int _numberOfPillars;
        private double[] _currentGuess;
        private double[] _currentPVs;
        private double[][] _jacobian;
        private ICurrencyProvider _currencyProvider;

        public IPriceCurve Solve(List<IAssetInstrument> instruments, IPriceCurve curve, List<IPriceCurve> dependencies, IIrCurve discountCurve, DateTime buildDate, ICurrencyProvider currencyProvider, ICalendarProvider calendarProvider)
        {
            dependencies ??= new List<IPriceCurve>();
            _currencyProvider = currencyProvider;
            _curveGenericInstruments = instruments;
            _numberOfInstruments = _curveGenericInstruments.Count;
            _pillars = curve.PillarDates;
            _numberOfPillars = curve.PillarDates.Length;
            _discountCurve = discountCurve;
            _buildDate = buildDate;

            var fm = new FundingModel(buildDate, new[] { discountCurve }, currencyProvider, calendarProvider);
            var model = new AssetFxModel(buildDate, fm);
            model.AddPriceCurves(dependencies.ToDictionary(x => x.Name, x => x));
            model.AddPriceCurve(curve.Name, curve);

            _currentGuess = instruments.Select(i=>i.ParRate(model)).ToArray();
            _currentCurve = curve;
            _currentPVs = ComputePVsGeneric(model);

            ComputeJacobianGeneric(model);

            for (var i = 0; i < MaxItterations; i++)
            {
                ComputeNextGuess();
                _currentCurve = new BasicPriceCurve(_buildDate, _pillars, _currentGuess, _currentCurve.CurveType, _currencyProvider)
                {
                    Name = _currentCurve.Name,
                };
                model.AddPriceCurve(_currentCurve.Name, _currentCurve);

                _currentPVs = ComputePVsGeneric(model);
                if (_currentPVs.Max(x => System.Math.Abs(x)) < Tollerance)
                {
                    UsedItterations = i + 1;
                    break;
                }
                ComputeJacobianGeneric(model);
            }

            return _currentCurve;
        }


        public IPriceCurve Solve(List<AsianSwapStrip> instruments, List<DateTime> pillars, IIrCurve discountCurve, DateTime buildDate, ICurrencyProvider currencyProvider)
        {
            _currencyProvider = currencyProvider;
            _curveInstruments = instruments;
            _pillars = pillars.ToArray();
            _numberOfInstruments = _curveInstruments.Count;
            _numberOfPillars = pillars.Count;
            _discountCurve = discountCurve;
            _buildDate = buildDate;

            _currentGuess = Enumerable.Repeat(instruments.Average(x => x.Swaplets.Average(s => s.Strike)), _numberOfPillars).ToArray();
            _currentCurve = new SparsePriceCurve(_buildDate, _pillars, _currentGuess, SparsePriceCurveType.Coal, currencyProvider, null);
            _currentPVs = ComputePVs();

            ComputeJacobian();

            for (var i = 0; i < MaxItterations; i++)
            {
                ComputeNextGuess();
                _currentCurve = new SparsePriceCurve(_buildDate, _pillars, _currentGuess, SparsePriceCurveType.Coal, _currencyProvider);

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

                _currentCurve = new SparsePriceCurve(_buildDate, _pillars, _currentGuess.Select((g, ix) => ix == i ? g + JacobianBump : g).ToArray(), SparsePriceCurveType.Coal, _currencyProvider);
                var bumpedPVs = ComputePVs();

                for (var j = 0; j < bumpedPVs.Length; j++)
                {
                    _jacobian[i][j] = (bumpedPVs[j] - _currentPVs[j]) / JacobianBump;
                }
            }
        }

        private void ComputeJacobianGeneric(IAssetFxModel model)
        {
            _jacobian = Math.Matrix.DoubleArrayFunctions.MatrixCreate(_numberOfPillars, _numberOfPillars);

            for (var i = 0; i < _numberOfPillars; i++)
            {
                _currentCurve = new BasicPriceCurve(_buildDate, _pillars, _currentGuess.Select((g, ix) => ix == i ? g + JacobianBump : g).ToArray(), _currentCurve.CurveType, _currencyProvider)
                {
                    Name = _currentCurve.Name
                };

                model.AddPriceCurve(_currentCurve.Name, _currentCurve);

                var bumpedPVs = ComputePVsGeneric(model);

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
                o[i] = SwapPv(_currentCurve, _curveInstruments[i], _discountCurve);
            }
            return o;
        }

        private double[] ComputePVsGeneric(IAssetFxModel model)
        {
            var o = new double[_numberOfInstruments];
            for (var i = 0; i < o.Length; i++)
            {
                var (pv, ccy, tradeId, tradeType) = AssetProductEx.ComputePV(_curveGenericInstruments[i], model, _curveGenericInstruments[i].Currency);
                o[i] = pv;    
            }
            return o;
        }

        public static double SwapPv(IPriceCurve priceCurve, AsianSwapStrip swap, IIrCurve discountCurve)
        {

            var swapletPVs = swap.Swaplets.Select(s =>
            (priceCurve.GetAveragePriceForDates(s.FixingDates.AddPeriod(RollType.F, s.FixingCalendar, s.SpotLag)) - s.Strike)
            * (s.Direction == TradeDirection.Long ? 1.0 : -1.0)
            * discountCurve.GetDf(priceCurve.BuildDate, s.PaymentDate));

            return swapletPVs.Sum();
        }
    }
}

