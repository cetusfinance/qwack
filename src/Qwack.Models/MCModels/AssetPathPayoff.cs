using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Instruments;
using Qwack.Core.Instruments.Asset;
using Qwack.Core.Instruments.Funding;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Math;
using Qwack.Math.Extensions;
using Qwack.Paths;
using Qwack.Paths.Features;
using Qwack.Paths.Regressors;

namespace Qwack.Models.MCModels
{
    public class AssetPathPayoff : IPathProcess, IRequiresFinish, IAssetPathPayoff
    {
        private object _locker = new object();

        private List<DateTime> _asianDates;
        private List<DateTime> _asianFxDates;
        private double _strike;
        private double _notional;
        private string _assetName;
        private string _fxName;
        private string _discountCurve;
        private DateTime _payDate;
        private Currency _ccy;
        private int _assetIndex;
        private int _fxIndex;
        private int[] _dateIndexes;
        private int[] _fxDateIndexes;
        private int _payDateIx;
        private Vector<double>[] _results;
        private bool _isComplete;
        private bool _isCompo;
        private OptionType _optionType;
        private FxConversionType _fxType;
        private int _rawNumberOfPaths;

        private List<IAssetPathPayoff> _subInstruments;

        private static readonly Vector<double> _one = new Vector<double>(1.0);
        private readonly ICurrencyProvider _currencyProvider;
        private readonly ICalendarProvider _calendarProvider;

        public string RegressionKey => _fxType == FxConversionType.None ? _assetName : $"{_assetName}*{_fxName}";

        private IAssetFxModel _vanillaModel;

        public IAssetFxModel VanillaModel
        {
            get => _vanillaModel;
            set
            {
                _vanillaModel = value;
                switch (AssetInstrument)
                {
                    case BackPricingOption bpo:
                        var bpob = _subInstruments.First() as Paths.Payoffs.BackPricingOption;
                        bpob.VanillaModel = value;
                        break;
                    case MultiPeriodBackpricingOption mpbpo:
                        var mbpob = _subInstruments.First() as Paths.Payoffs.MultiPeriodBackPricingOption;
                        mbpob.VanillaModel = value;
                        break;
                }
            }
        }
        public LinearAveragePriceRegressor[] Regressors { get; private set; }
        
        public void SetRegressor(LinearAveragePriceRegressor regressor)
        {
            var match = Regressors.Where(x => x == regressor);
            if (!match.Any())
                throw new Exception("Attempted to set a regressor but no match could be found");

            for (var i = 0; i < Regressors.Length; i++)
            {
                foreach (var m in match)
                    if (Regressors[i] == m) Regressors[i] = m;
            }

            switch(AssetInstrument)
            {
                case BackPricingOption bpo:
                    var bpob = _subInstruments.First() as Paths.Payoffs.BackPricingOption;
                    if (bpob.SettlementRegressor == regressor) bpob.SettlementRegressor = regressor;
                    if (bpob.AverageRegressor == regressor) bpob.AverageRegressor = regressor;
                    break;
                case MultiPeriodBackpricingOption mpbpo:
                    var mbpo = _subInstruments.First() as Paths.Payoffs.MultiPeriodBackPricingOption;
                    if (mbpo.SettlementRegressor == regressor) mbpo.SettlementRegressor = regressor;
                    for(var i=0;i<mbpo.AverageRegressors.Length;i++)
                        if (mbpo.AverageRegressors[i] == regressor) mbpo.AverageRegressors[i] = regressor;
                    break;
            }
        }

        public AssetPathPayoff(IAssetInstrument assetInstrument, ICurrencyProvider currencyProvider, ICalendarProvider calendarProvider, Currency simulationCcy)
        {
            AssetInstrument = assetInstrument;
            _currencyProvider = currencyProvider;
            _calendarProvider = calendarProvider;
            switch (AssetInstrument)
            {
                case AsianOption ao:
                    _asianDates = ao.FixingDates.ToList();
                    _strike = ao.Strike;
                    _notional = ao.Notional * (ao.Direction == TradeDirection.Long ? 1.0 : -1.0);
                    _optionType = ao.CallPut;
                    _fxType = ao.FxConversionType;
                    _ccy = ao.PaymentCurrency;
                    if (_fxType == FxConversionType.None && simulationCcy != _ccy)
                    {
                        _fxType = FxConversionType.SettleOtherCurrency;
                    }
                    _fxName = _fxType == FxConversionType.None ? null : $"USD/{_ccy}";
                    _asianFxDates = ao.FxFixingDates?.ToList()??_asianDates;
                    _discountCurve = ao.DiscountCurve;
                    _payDate = ao.PaymentDate;
                    break;
                case AsianSwap asw:
                    _asianDates = asw.FixingDates.ToList();
                    _strike = asw.Strike;
                    _notional = asw.Notional * (asw.Direction == TradeDirection.Long ? 1.0 : -1.0);
                    _optionType = OptionType.Swap;
                    _fxType = asw.FxConversionType;
                    _ccy = asw.PaymentCurrency;
                    if (_fxType == FxConversionType.None && simulationCcy != _ccy)
                    {
                        _fxType = FxConversionType.SettleOtherCurrency;
                        _fxName = _fxType == FxConversionType.None ? null : $"{asw.PaymentCurrency.Ccy}/{simulationCcy}";                        
                    }
                    else
                    {
                        _fxName = _fxType == FxConversionType.None ? null : $"USD/{asw.PaymentCurrency.Ccy}";
                    }
                    _asianFxDates = asw.FxFixingDates?.ToList() ?? _asianDates;
                    _discountCurve = asw.DiscountCurve;
                    _payDate = asw.PaymentDate;
                    break;
                case AsianSwapStrip asws:
                    _subInstruments = asws.Swaplets.Select(x => (IAssetPathPayoff)new AssetPathPayoff(x, _currencyProvider, _calendarProvider, simulationCcy)).ToList();
                    break;
                case FxVanillaOption fxeo:
                    _asianDates = new List<DateTime> { fxeo.ExpiryDate };
                    _strike = fxeo.Strike;
                    _notional = fxeo.DomesticQuantity;
                    _optionType = fxeo.CallPut;
                    _asianFxDates = _asianDates;
                    _discountCurve = fxeo.ForeignDiscountCurve;
                    _payDate = fxeo.DeliveryDate;
                    _ccy = fxeo.PaymentCurrency;
                    _fxType = (simulationCcy != _ccy) ? FxConversionType.SettleOtherCurrency : FxConversionType.None;
                    _fxName = _fxType == FxConversionType.None ? null : $"USD/{_ccy}";
                    break;
                case EuropeanBarrierOption ebo:
                    _subInstruments = new List<IAssetPathPayoff>
                    {
                        new Paths.Payoffs.EuropeanBarrierOption(ebo.AssetId,ebo.BarrierObservationStartDate,ebo.BarrierObservationEndDate,ebo.ExpiryDate,ebo.CallPut,ebo.Strike,ebo.Barrier,ebo.DiscountCurve,ebo.Currency,ebo.PaymentDate,ebo.Notional,ebo.BarrierSide,ebo.BarrierType)
                    };
                    break;
                case EuropeanOption eo:
                    _asianDates = new List<DateTime> { eo.ExpiryDate };
                    _strike = eo.Strike;
                    _notional = eo.Notional * (eo.Direction == TradeDirection.Long ? 1.0 : -1.0);
                    _optionType = eo.CallPut;
                    _fxType = eo.FxConversionType;
                    _ccy = eo.PaymentCurrency;
                    if (_fxType == FxConversionType.None && simulationCcy != _ccy)
                    {
                        _fxType = FxConversionType.SettleOtherCurrency;
                    }
                    _fxName = _fxType == FxConversionType.None ? null : $"USD/{_ccy}";
                    _asianFxDates = _asianDates;
                    _discountCurve = eo.DiscountCurve;
                    _payDate = eo.PaymentDate;
                    break;
                case OneTouchOption ot:
                    _subInstruments = new List<IAssetPathPayoff>
                    {
                        new Paths.Payoffs.OneTouch(ot.AssetId,ot.BarrierObservationStartDate,ot.BarrierObservationEndDate
                        ,ot.Barrier,ot.DiscountCurve,ot.Currency,ot.PaymentDate,ot.Notional,ot.BarrierSide,ot.BarrierType)
                    };
                    break;
                case DoubleNoTouchOption dnt:
                    _subInstruments = new List<IAssetPathPayoff>
                    {
                        new Paths.Payoffs.DoubleNoTouch(dnt.AssetId,dnt.BarrierObservationStartDate,dnt.BarrierObservationEndDate
                        ,dnt.BarrierDown,dnt.BarrierUp,dnt.DiscountCurve,dnt.Currency,dnt.PaymentDate,dnt.Notional,dnt.BarrierType)
                    };
                    break;
                case Forward f:
                    _asianDates = new List<DateTime> { f.ExpiryDate };
                    _strike = f.Strike;
                    _notional = f.Notional * (f.Direction == TradeDirection.Long ? 1.0 : -1.0);
                    _optionType = OptionType.Swap;
                    _asianFxDates = _asianDates;
                    _discountCurve = f.DiscountCurve;
                    _payDate = f.PaymentDate;
                    _ccy = f.PaymentCurrency;
                    _fxType = f.FxConversionType;
                    _fxName = _fxType == FxConversionType.None ? null : $"USD/{_ccy}";
                    break;
                case FxForward fxf:
                    var pair = fxf.Pair.FxPairFromString(_currencyProvider, _calendarProvider);
                    _asianDates = new List<DateTime> { fxf.DeliveryDate.SubtractPeriod(RollType.P, pair.SettlementCalendar, pair.SpotLag) };
                    _strike = fxf.Strike;
                    _notional = fxf.DomesticQuantity;
                    _optionType = OptionType.Swap;
                    _asianFxDates = _asianDates;
                    _discountCurve = fxf.ForeignDiscountCurve;
                    _payDate = fxf.DeliveryDate;
                    _ccy = fxf.PaymentCurrency;
                    _fxType = (simulationCcy != _ccy) ? FxConversionType.SettleOtherCurrency : FxConversionType.None;
                    _fxName = _fxType == FxConversionType.None ? null : $"USD/{_ccy}";
                    break;
                case AsianBasisSwap abs:
                    _subInstruments = abs.PaySwaplets.Select(x => (IAssetPathPayoff)new AssetPathPayoff(x,_currencyProvider,_calendarProvider, simulationCcy))
                        .Concat(abs.RecSwaplets.Select(x => (IAssetPathPayoff)new AssetPathPayoff(x, _currencyProvider, _calendarProvider, simulationCcy)))
                        .ToList();
                    break;
                case AsianLookbackOption alb:
                    _subInstruments = new List<IAssetPathPayoff>
                    {
                        new Paths.Payoffs.LookBackOption(alb.AssetId, alb.FixingDates.ToList(), alb.CallPut, alb.DiscountCurve, alb.PaymentCurrency, alb.PaymentDate, alb.Notional)
                    };
                    break;
                case BackPricingOption bpo:
                    var bp = new Paths.Payoffs.BackPricingOption(bpo.AssetId, bpo.FixingDates.ToList(), bpo.DecisionDate, bpo.SettlementDate, bpo.SettlementDate, bpo.CallPut, bpo.DiscountCurve, bpo.PaymentCurrency, bpo.Notional)
                    { VanillaModel = VanillaModel };
                    _subInstruments = new List<IAssetPathPayoff>
                    {
                        bp
                    };
                    if (bp.AverageRegressor != null)
                        Regressors = new[] { bp.AverageRegressor, bp.SettlementRegressor };
                    else
                        Regressors = new[] { bp.SettlementRegressor };
                    break;
                case MultiPeriodBackpricingOption mbpo:
                    var mbp = new Paths.Payoffs.MultiPeriodBackPricingOption(mbpo.AssetId, mbpo.FixingDates, mbpo.DecisionDate, mbpo.SettlementDate, mbpo.SettlementDate, mbpo.CallPut, mbpo.DiscountCurve, mbpo.PaymentCurrency, mbpo.Notional)
                    { VanillaModel = VanillaModel };
                    _subInstruments = new List<IAssetPathPayoff>
                    {
                        mbp
                    };
                    if (mbp.AverageRegressors != null)
                        Regressors = mbp.AverageRegressors.Where(x => x != null).Concat(new[] { mbp.SettlementRegressor }).ToArray();
                    else
                        Regressors = new[] { mbp.SettlementRegressor };
                    break;
            }
            _isCompo = _fxType == FxConversionType.ConvertThenAverage || _fxType == FxConversionType.AverageThenConvert;
            _assetName = AssetInstrument.AssetIds.Any() ? 
                AssetInstrument.AssetIds.First() :
                (AssetInstrument is FxVanillaOption fxo ? fxo.PairStr : null);
        }

        public bool IsComplete => _isComplete;

        public IAssetInstrument AssetInstrument { get; private set; }

        public void Finish(IFeatureCollection collection)
        {
            if (_subInstruments != null)
            {
                foreach (var ins in _subInstruments)
                {
                    ins.Finish(collection);
                }
                _isComplete = true;
                return;
            }

            var dims = collection.GetFeature<IPathMappingFeature>();
            _assetIndex = dims.GetDimension(_assetName);

            if (_assetIndex < 0)
                throw new Exception($"Asset index {_assetName} not found in MC engine");

            if (_isCompo || _fxType == FxConversionType.SettleOtherCurrency)
            {
                _fxIndex = dims.GetDimension(_fxName);
                if (_fxIndex < 0)
                    throw new Exception($"Fx index {_fxName} not found in MC engine");
            }
            var dates = collection.GetFeature<ITimeStepsFeature>();
            _dateIndexes = new int[_asianDates.Count];
            _fxDateIndexes = new int[_asianFxDates.Count];
            for (var i = 0; i < _asianDates.Count; i++)
            {
                _dateIndexes[i] = dates.GetDateIndex(_asianDates[i]);
            }

            if (_isCompo)
                for (var i = 0; i < _asianDates.Count; i++)
                {
                    _fxDateIndexes[i] = dates.GetDateIndex(_asianFxDates[i]);
                }

            _payDateIx = dates.GetDateIndex(_payDate);

            _isComplete = true;

            var engine = collection.GetFeature<IEngineFeature>();
            _results = new Vector<double>[engine.RoundedNumberOfPaths / Vector<double>.Count];
            _rawNumberOfPaths = engine.NumberOfPaths;
        }

        public void Process(IPathBlock block)
        {
            if(_subInstruments!=null)
            {
                foreach(var ins in _subInstruments)
                {
                    ins.Process(block);
                }
                return;
            }

            var blockBaseIx = block.GlobalPathIndex;

            for (var path = 0; path < block.NumberOfPaths; path += Vector<double>.Count)
            { 
                var steps = block.GetStepsForFactor(path, _assetIndex);
                var stepsfx = (_isCompo || _fxType == FxConversionType.SettleOtherCurrency) ? block.GetStepsForFactor(path, _fxIndex) : null;

                var finalValues = new Vector<double>(0.0);
                var finalValuesFx = new Vector<double>(0.0);

                if (_fxType == FxConversionType.ConvertThenAverage)
                {
                    for (var i = 0; i < _dateIndexes.Length; i++)
                        finalValues += steps[_dateIndexes[i]] * (_isCompo ? stepsfx[_dateIndexes[i]] : _one);

                    finalValues = finalValues / new Vector<double>(_dateIndexes.Length);
                }
                else
                {
                    for (var i = 0; i < _dateIndexes.Length; i++)
                        finalValues += steps[_dateIndexes[i]];

                    finalValues = finalValues / new Vector<double>(_dateIndexes.Length);

                    if (_isCompo) //Average-then-convert
                    {
                        for (var i = 0; i < _fxDateIndexes.Length; i++)
                            finalValuesFx += stepsfx[_fxDateIndexes[i]];
                        
                        finalValuesFx = finalValuesFx / new Vector<double>(_fxDateIndexes.Length);
                        finalValues = finalValues * finalValuesFx;
                    }
                }

                switch (_optionType)
                {
                    case OptionType.Call:
                        finalValues = finalValues - new Vector<double>(_strike);
                        finalValues = Vector.Max(new Vector<double>(0), finalValues) * new Vector<double>(_notional);
                        break;
                    case OptionType.Put:
                        finalValues = new Vector<double>(_strike) - finalValues;
                        finalValues = Vector.Max(new Vector<double>(0), finalValues) * new Vector<double>(_notional);
                        break;
                    case OptionType.Swap:
                        finalValues = finalValues  - new Vector<double>(_strike);
                        finalValues = finalValues * new Vector<double>(_notional);
                        break;
                }

                if(_fxType==FxConversionType.SettleOtherCurrency)
                {
                    finalValues = finalValues * stepsfx[_payDateIx];
                }

                var resultIx = (blockBaseIx + path) / Vector<double>.Count;
                _results[resultIx] = finalValues;
            }
        }

        public void SetupFeatures(IFeatureCollection pathProcessFeaturesCollection)
        {
            if (_subInstruments != null)
            {
                foreach (var ins in _subInstruments)
                {
                    ins.SetupFeatures(pathProcessFeaturesCollection);
                }
                return;
            }

            var dates = pathProcessFeaturesCollection.GetFeature<ITimeStepsFeature>();
            dates.AddDates(_asianDates);
            if (_isCompo)
                dates.AddDates(_asianFxDates);
            dates.AddDate(_payDate);
        }

        public double AverageResult => ResultsByPath.Average();

        public double[] ResultsByPath
        {
            get {
                var vecLen = Vector<double>.Count;
                var results = new double[_rawNumberOfPaths];
                for (var i = 0; i < _results.Length; i++)
                {
                    for (var j = 0; j < vecLen; j++)
                    {
                        var c = i * vecLen + j;
                        if (c >= results.Length)
                            break;
                        results[c] = _results[i][j];
                    }
                }
                return results;
            }
        }

        public double ResultStdError => ResultsByPath.StdDev();

        public CashFlowSchedule ExpectedFlows(IAssetFxModel model)
        {
            if (_subInstruments != null)
            {
                var o = new CashFlowSchedule
                {
                    Flows = new List<CashFlow>(),
                };

                foreach (var ins in _subInstruments)
                {
                    o.Flows.AddRange(ins.ExpectedFlows(model).Flows);
                }
                return o;
            }

            var ar = AverageResult;
            return new CashFlowSchedule
            {
                Flows = new List<CashFlow>
                {
                    new CashFlow
                    {
                        Fv = ar,
                        Pv = ar * model.FundingModel.Curves[_discountCurve].GetDf(model.BuildDate,_payDate),
                        Currency = _ccy,
                        FlowType =  FlowType.FixedAmount,
                        SettleDate = _payDate,
                        NotionalByYearFraction = 1.0
                    }
                }
            };
        }

        public CashFlowSchedule[] ExpectedFlowsByPath(IAssetFxModel model)
        {
            if (_subInstruments != null)
            {
                CashFlowSchedule[] o = null;

                foreach (var ins in _subInstruments)
                {
                    var byPathForIns = ins.ExpectedFlowsByPath(model);
                    if (o == null)
                        o = byPathForIns;
                    else
                    {
                        for (var i = 0; i < o.Length; i++)
                        {
                            o[i].Flows.AddRange(byPathForIns[i].Flows);
                        }
                    }
                }
                return o;
            }

            var df = model.FundingModel.Curves[_discountCurve].GetDf(model.BuildDate, _payDate);

            return ResultsByPath.Select(x => new CashFlowSchedule
            {
                Flows = new List<CashFlow>
                {
                    new CashFlow
                    {
                        Fv = x,
                        Pv = x * df,
                        Currency = _ccy,
                        FlowType =  FlowType.FixedAmount,
                        SettleDate = _payDate,
                        NotionalByYearFraction = 1.0
                    }
                }
            }).ToArray(); 
        }

        
    }
}
