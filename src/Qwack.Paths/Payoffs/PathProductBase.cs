using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Qwack.Core.Basic;
using Qwack.Core.Instruments;
using Qwack.Core.Models;
using Qwack.Math;
using Qwack.Math.Extensions;
using Qwack.Paths.Features;

namespace Qwack.Paths.Payoffs
{
    public abstract class PathProductBase : IPathProcess, IRequiresFinish, IAssetPathPayoff
    {
        protected readonly string _discountCurve;
        protected readonly Currency _ccy;
        protected readonly DateTime _payDate;
        protected readonly string _assetName;
        protected int _assetIndex;
        protected int[] _dateIndexes;
        protected Vector<double>[] _results;
        protected Vector<double> _notional;
        protected Vector<double> _zero = new(0.0);
        protected bool _isComplete;

        protected bool _requiresConversionToSimCcy;
        protected bool _requiresConversionToSimCcyInverted;
        protected string _conversionToSimCcyName;
        protected int _conversionToSimCcyIx;

        public virtual string RegressionKey => _assetName;


        public PathProductBase(string assetName, string discountCurve, Currency ccy, DateTime payDate, double notional, Currency simulationCurrency)
        {
            _discountCurve = discountCurve;
            _ccy = ccy;
            _payDate = payDate;

            _assetName = assetName;
            _notional = new Vector<double>(notional);
            SimulationCcy = simulationCurrency;

            _requiresConversionToSimCcy = SimulationCcy != _ccy;
            if (_requiresConversionToSimCcy)
            {
                _conversionToSimCcyName = $"{SimulationCcy}/{_ccy}";
            }
        }

        public bool IsComplete => _isComplete;

        public IAssetInstrument AssetInstrument { get; private set; }

        public Currency SimulationCcy { get; private set; }
        public abstract void Finish(IFeatureCollection collection);

        public abstract void Process(IPathBlock block);

        public abstract void SetupFeatures(IFeatureCollection pathProcessFeaturesCollection);

        public double AverageResult => _results.Select(x =>
        {
            var vec = new double[Vector<double>.Count];
            x.CopyTo(vec);
            return vec.Average();
        }).Average();

        public double[] ResultsByPath => _results.SelectMany(x => x.Values()).ToArray();

        public double ResultStdError => _results.SelectMany(x =>
        {
            var vec = new double[Vector<double>.Count];
            x.CopyTo(vec);
            return vec;
        }).StdDev();


        internal void ConvertToSimCcyIfNeeded(IPathBlock block, int pathId, Vector<double> values, int payDateIndex)
        {
            if (_requiresConversionToSimCcy)
            {
                var stepsFxConv = block.GetStepsForFactor(pathId, _conversionToSimCcyIx);
                if (_requiresConversionToSimCcyInverted)
                    values *= stepsFxConv[payDateIndex];
                else
                    values /= stepsFxConv[payDateIndex];
            }
        }

        internal void SetupForCcyConversion(IFeatureCollection collection)
        {
            if (_requiresConversionToSimCcy)
            {
                var dims = collection.GetFeature<IPathMappingFeature>();

                _conversionToSimCcyIx = dims.GetDimension(_conversionToSimCcyName);
                if (_conversionToSimCcyIx == -1)
                {
                    _conversionToSimCcyIx = dims.GetDimension($"{_conversionToSimCcyName.Substring(4, 3)}/{_conversionToSimCcyName.Substring(0, 3)}");
                    if (_conversionToSimCcyIx == -1)
                    {
                        throw new Exception($"Unable to find process to convert currency from {_ccy} to {SimulationCcy}");
                    }
                    _requiresConversionToSimCcyInverted = true;
                }
            }
        }

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
                        YearFraction = 1.0
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
                        YearFraction = 1.0
                    }
                }
            }).ToArray();

        }
    }
}
