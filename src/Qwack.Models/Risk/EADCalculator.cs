using System;
using System.Collections.Generic;
using System.Linq;
using Qwack.Core.Models;
using Qwack.Models.Models;
using Qwack.Core.Instruments;
using Qwack.Core.Basic;
using Qwack.Utils.Parallel;
using Qwack.Core.Cubes;
using Qwack.Transport.BasicTypes;

namespace Qwack.Models.Risk
{
    public class EADCalculator
    {
        public EADCalculator(Portfolio portfolio, double counterpartyRiskWeight, Dictionary<string, string> assetIdToTypeMap,
            Dictionary<string, SaCcrAssetClass> typeToAssetClassMap, Currency reportingCurrency, IAssetFxModel assetFxModel, DateTime[] calculationDates, 
            double[] epeValues, ICurrencyProvider currencyProvider)
        {
            _portfolio = portfolio;
            _counterpartyRiskWeight = counterpartyRiskWeight;
            _assetIdToTypeMap = assetIdToTypeMap;
            _typeToAssetClassMap = typeToAssetClassMap;
            _reportingCurrency = reportingCurrency;
            _assetFxModel = assetFxModel;
            _calculationDates = calculationDates;
            _epeValues = epeValues;
            _currencyProvider = currencyProvider;
            _endDate = portfolio.LastSensitivityDate;
        }

        public Dictionary<DateTime, double> EAD => _ead.OrderBy(x=>x.Key).ToDictionary(x => x.Key, x => x.Value );

        public ICube ResultCube()
        {
            var cube = new ResultCube();
            var dataTypes = new Dictionary<string, Type>
            {
                { "ExposureDate", typeof(DateTime) }
            };
            cube.Initialize(dataTypes);

            foreach (var kv in EAD)
            {
                cube.AddRow(new object[] { kv.Key }, kv.Value);
            }
            return cube;
        }

        private object _threadLock = new object();
        private readonly Portfolio _portfolio;
        private readonly double _counterpartyRiskWeight;
        private readonly Dictionary<string, string> _assetIdToTypeMap;
        private readonly Dictionary<string, SaCcrAssetClass> _typeToAssetClassMap;
        private readonly Currency _reportingCurrency;
        private IAssetFxModel _assetFxModel;
        private readonly DateTime[] _calculationDates;
        private readonly double[] _epeValues;
        private readonly ICurrencyProvider _currencyProvider;
        private readonly DateTime _endDate;


        private readonly Dictionary<DateTime, double> _ead = new Dictionary<DateTime, double>();

        public void Process()
        {
            IAssetFxModel model;
            if(_assetFxModel.FundingModel.FxMatrix.BaseCurrency!=_reportingCurrency)
            {
                var newFm = FundingModel.RemapBaseCurrency(_assetFxModel.FundingModel, _reportingCurrency, _currencyProvider);
                model = _assetFxModel.Clone(newFm);
            }
            else
            {
                model = _assetFxModel.Clone();
            }

            var currentFixingDate = _assetFxModel.BuildDate;
            while (currentFixingDate <= _endDate)
            {
                currentFixingDate = currentFixingDate.AddDays(1);
                _assetFxModel = _assetFxModel.RollModel(currentFixingDate, _currencyProvider);
            }

            foreach (ISaCcrEnabledCommodity ins in _portfolio.Instruments)
            {
                ins.CommodityType = _assetIdToTypeMap[(ins as IAssetInstrument).AssetIds.First()];
                ins.AssetClass = _typeToAssetClassMap[ins.CommodityType];
            }

            ParallelUtils.Instance.For(0,_calculationDates.Length,1,i=> 
            {
                var d = _calculationDates[i];
                var epe = _epeValues[i];
                var newModel = _assetFxModel.Clone();
                newModel.OverrideBuildDate(d);

                var ead = SaCcrHelper.SaCcrEad(_portfolio, newModel, epe);
                var capital = _counterpartyRiskWeight * ead;
                if (!_ead.ContainsKey(d))
                    lock (_threadLock)
                    {
                        if (!_ead.ContainsKey(d))
                            _ead.Add(d, 0.0);
                    }
                if (double.IsNaN(capital) || double.IsInfinity(capital))
                    throw new Exception("Invalid capital generated");

                lock (_threadLock)
                {
                    _ead[d] += capital;
                }

            }).Wait();
        }

        public void Process(Dictionary<DateTime,IAssetFxModel> models)
        {
            ParallelUtils.Instance.For(0, _calculationDates.Length, 1, i =>
            {
                var d = _calculationDates[i];
                var epe = _epeValues[i];
                var newModel = models[d];

                var ead = SaCcrHelper.SaCcrEad(_portfolio, newModel, epe);
                var capital = _counterpartyRiskWeight * ead;
                if (!_ead.ContainsKey(d))
                    lock (_threadLock)
                    {
                        if (!_ead.ContainsKey(d))
                            _ead.Add(d, 0.0);
                    }
                if (double.IsNaN(capital) || double.IsInfinity(capital))
                    throw new Exception("Invalid capital generated");

                lock (_threadLock)
                {
                    _ead[d] += capital;
                }

            }).Wait();
        }
    }
}

