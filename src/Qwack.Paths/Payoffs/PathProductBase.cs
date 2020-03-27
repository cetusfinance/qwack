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
        protected Vector<double> _zero = new Vector<double>(0.0);
        protected bool _isComplete;

        public virtual string RegressionKey => _assetName;


        public PathProductBase(string assetName, string discountCurve, Currency ccy, DateTime payDate, double notional)
        {
            _discountCurve = discountCurve;
            _ccy = ccy;
            _payDate = payDate;

            _assetName = assetName;
            _notional = new Vector<double>(notional);
        }

        public bool IsComplete => _isComplete;

        public IAssetInstrument AssetInstrument { get; private set; }

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
