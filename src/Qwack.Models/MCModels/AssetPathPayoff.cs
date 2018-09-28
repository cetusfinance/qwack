using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Instruments;
using Qwack.Core.Instruments.Asset;
using Qwack.Core.Models;
using Qwack.Math;
using Qwack.Math.Extensions;
using Qwack.Paths;
using Qwack.Paths.Features;

namespace Qwack.Models.MCModels
{
    public class AssetPathPayoff : IPathProcess, IRequiresFinish, IAssetPathPayoff
    {
        private List<DateTime> _asianDates;
        private List<DateTime> _asianFxDates;
        private readonly double _strike;
        private readonly double _notional;
        private readonly string _assetName;
        private readonly string _fxName;
        private readonly string _discountCurve;
        private readonly DateTime _payDate;
        private readonly Currency _ccy;
        private int _assetIndex;
        private int _fxIndex;
        private int[] _dateIndexes;
        private int[] _fxDateIndexes;
        private List<Vector<double>> _results = new List<Vector<double>>();
        private bool _isComplete;
        private readonly bool _isCompo;
        private readonly OptionType _optionType;
        private readonly FxConversionType _fxType;

        private readonly List<AssetPathPayoff> _subInstruments;

        private readonly Vector<double> _one = new Vector<double>(1.0);

        public AssetPathPayoff(IAssetInstrument assetInstrument)
        {
            AssetInstrument = assetInstrument;
            switch (AssetInstrument)
            {
                case AsianOption ao:
                    _asianDates = ao.FixingDates.ToList();
                    _strike = ao.Strike;
                    _notional = ao.Notional * (ao.Direction == TradeDirection.Long ? 1.0 : -1.0);
                    _optionType = ao.CallPut;
                    _fxName = ao.FxConversionType == FxConversionType.None ? null : $"USD/{ao.PaymentCurrency.Ccy}";
                    _asianFxDates = ao.FxFixingDates?.ToList()??_asianDates;
                    _discountCurve = ao.DiscountCurve;
                    _payDate = ao.PaymentDate;
                    _ccy = ao.PaymentCurrency;
                    _fxType = ao.FxConversionType;
                    break;
                case AsianSwap asw:
                    _asianDates = asw.FixingDates.ToList();
                    _strike = asw.Strike;
                    _notional = asw.Notional * (asw.Direction == TradeDirection.Long ? 1.0 : -1.0);
                    _optionType = OptionType.Swap;
                    _fxName = asw.FxConversionType == FxConversionType.None ? null : $"USD/{asw.PaymentCurrency.Ccy}";
                    _asianFxDates = asw.FxFixingDates?.ToList() ?? _asianDates;
                    _discountCurve = asw.DiscountCurve;
                    _payDate = asw.PaymentDate;
                    _ccy = asw.PaymentCurrency;
                    _fxType = asw.FxConversionType;
                    break;
                case AsianSwapStrip asws:
                    _subInstruments = asws.Swaplets.Select(x => new AssetPathPayoff(x)).ToList();
                    break;
                case EuropeanOption eo:
                    _asianDates = new List<DateTime> { eo.ExpiryDate };
                    _strike = eo.Strike;
                    _notional = eo.Notional * (eo.Direction == TradeDirection.Long ? 1.0 : -1.0);
                    _optionType = eo.CallPut;
                    _fxName = eo.FxConversionType == FxConversionType.None ? null : $"USD/{eo.PaymentCurrency.Ccy}";
                    _asianFxDates = _asianDates;
                    _discountCurve = eo.DiscountCurve;
                    _payDate = eo.PaymentDate;
                    _ccy = eo.PaymentCurrency;
                    _fxType = eo.FxConversionType;
                    break;
                case Forward f:
                    _asianDates = new List<DateTime> { f.ExpiryDate };
                    _strike = f.Strike;
                    _notional = f.Notional * (f.Direction == TradeDirection.Long ? 1.0 : -1.0);
                    _optionType = OptionType.Swap;
                    _fxName = f.FxConversionType == FxConversionType.None ? null : $"USD/{f.PaymentCurrency.Ccy}";
                    _asianFxDates = _asianDates;
                    _discountCurve = f.DiscountCurve;
                    _payDate = f.PaymentDate;
                    _ccy = f.PaymentCurrency;
                    _fxType = f.FxConversionType;
                    break;
                case AsianBasisSwap abs:
                    _subInstruments = abs.PaySwaplets.Select(x => new AssetPathPayoff(x))
                        .Concat(abs.RecSwaplets.Select(x => new AssetPathPayoff(x)))
                        .ToList();
                    break;
            }
            _isCompo = _fxName != null;
            _assetName = AssetInstrument.AssetIds.First();
        }

        public bool IsComplete => _isComplete;

        public IAssetInstrument AssetInstrument { get; }

        public void Finish(IFeatureCollection collection)
        {
            if (_subInstruments != null)
            {
                foreach (var ins in _subInstruments)
                {
                    ins.Finish(collection);
                }
                return;
            }

            var dims = collection.GetFeature<IPathMappingFeature>();
            _assetIndex = dims.GetDimension(_assetName);
            if (_isCompo)
                _fxIndex = dims.GetDimension(_fxName);

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

            _isComplete = true;
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

            for (var path = 0; path < block.NumberOfPaths; path += Vector<double>.Count)
            { 
                var steps = block.GetStepsForFactor(path, _assetIndex);
                var stepsfx = _isCompo ? block.GetStepsForFactor(path, _fxIndex) : null;

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

                _results.Add(finalValues);
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
        }

        public double AverageResult => _results.Select(x =>
        {
            var vec = new double[Vector<double>.Count];
            x.CopyTo(vec);
            return vec.Average();
        }).Average();

        private double[] ResultsByPath => _results.SelectMany(x => x.Values()).ToArray();

        public double ResultStdError => _results.SelectMany(x =>
        {
            var vec = new double[Vector<double>.Count];
            x.CopyTo(vec);
            return vec;
        }).StdDev();

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

            return ResultsByPath.Select(x => new CashFlowSchedule
            {
                Flows = new List<CashFlow>
                {
                    new CashFlow
                    {
                        Fv = x,
                        Pv = x * model.FundingModel.Curves[_discountCurve].GetDf(model.BuildDate,_payDate),
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
