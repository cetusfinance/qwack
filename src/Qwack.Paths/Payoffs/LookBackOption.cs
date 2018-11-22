using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Instruments;
using Qwack.Core.Models;
using Qwack.Math;
using Qwack.Math.Extensions;
using Qwack.Paths.Features;

namespace Qwack.Paths.Payoffs
{
    public class LookBackOption : IPathProcess, IRequiresFinish, IAssetPathPayoff
    {
        private object _locker = new object();

        private List<DateTime> _sampleDates;
        private readonly OptionType _callPut;
        private readonly string _discountCurve;
        private readonly Currency _ccy;
        private readonly DateTime _payDate;
        private readonly string _assetName;
        private int _assetIndex;
        private int[] _dateIndexes;
        private Vector<double>[] _results;
        private Vector<double> _notional;
        private bool _isComplete;

        public LookBackOption(string assetName, List<DateTime> sampleDates, OptionType callPut, string discountCurve, Currency ccy, DateTime payDate, double notional)
        {
            _sampleDates = sampleDates;
            _callPut = callPut;
            _discountCurve = discountCurve;
            _ccy = ccy;
            _payDate = payDate;
            _assetName = assetName;
            _notional = new Vector<double>(notional);
        }

        public bool IsComplete => _isComplete;

        public IAssetInstrument AssetInstrument { get; private set; }


        public void Finish(IFeatureCollection collection)
        {
            var dims = collection.GetFeature<IPathMappingFeature>();
            _assetIndex = dims.GetDimension(_assetName);

            var dates = collection.GetFeature<ITimeStepsFeature>();
            _dateIndexes = new int[_sampleDates.Count];
            for(var i = 0; i < _sampleDates.Count; i++)
            {
                _dateIndexes[i] = dates.GetDateIndex(_sampleDates[i]);
            }

            var engine = collection.GetFeature<IEngineFeature>();
            _results = new Vector<double>[engine.NumberOfPaths / Vector<double>.Count];
            _isComplete = true;
        }

        public void Process(IPathBlock block)
        {
            var blockBaseIx = block.GlobalPathIndex;

            if (_callPut==OptionType.C)
            {
                for (var path = 0; path < block.NumberOfPaths; path += Vector<double>.Count)
                {
                    var steps = block.GetStepsForFactor(path, _assetIndex);
                    var minValue = new Vector<double>(double.MaxValue);
                    for (var i = 0; i < _dateIndexes.Length; i++)
                    {
                        minValue = Vector.Min(steps[_dateIndexes[i]], minValue);
                    }
                    var lastValue = steps[_dateIndexes.Last()];
                    var payoff = (lastValue - minValue) * _notional;

                    var resultIx = (blockBaseIx + path) / Vector<double>.Count;
                    _results[resultIx] = payoff;
                }
            }
            else
            {
                for (var path = 0; path < block.NumberOfPaths; path += Vector<double>.Count)
                {
                    var steps = block.GetStepsForFactor(path, _assetIndex);
                    var maxValue = new Vector<double>(double.MinValue);
                    for (var i = 0; i < _dateIndexes.Length; i++)
                    {
                        maxValue = Vector.Max(steps[_dateIndexes[i]], maxValue);
                    }
                    var lastValue = steps[_dateIndexes.Last()];
                    var payoff = (maxValue - lastValue) * _notional;

                    var resultIx = (blockBaseIx + path) / Vector<double>.Count;
                    _results[resultIx] = payoff;
                }
            }
        }

        public void SetupFeatures(IFeatureCollection pathProcessFeaturesCollection)
        {
            var dates = pathProcessFeaturesCollection.GetFeature<ITimeStepsFeature>();
            dates.AddDates(_sampleDates);
        }

        public double AverageResult => _results.Select(x =>
        {
            var vec = new double[Vector<double>.Count];
            x.CopyTo(vec);
            return vec.Average();
        }).Average();

        public double[] ResultsByPath
        {
            get
            {
                var vecLen = Vector<double>.Count;
                var results = new double[_results.Length * vecLen];
                for (var i = 0; i < _results.Length; i++)
                {
                    for (var j = 0; j < vecLen; j++)
                    {
                        results[i * vecLen + j] = _results[i][j];
                    }
                }
                return results;
            }
        }

        public double ResultStdError => _results.SelectMany(x =>
        {
            var vec = new double[Vector<double>.Count];
            x.CopyTo(vec);
            return vec;
        }).StdDev();

        public CashFlowSchedule ExpectedFlows(IAssetFxModel model)
        {
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
