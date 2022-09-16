using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Qwack.Core.Basic;
using Qwack.Core.Instruments;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Paths.Features;

namespace Qwack.Models.MCModels
{
    public class FactorReturnPayoff : IPathProcess, IRequiresFinish
    {
        private int[] _dateIndexes;

        private Vector<double>[][][] _results;
        private bool _isComplete;
        private int _rawNumberOfPaths;

        private readonly List<IAssetPathPayoff> _subInstruments;

        private readonly string[] _assetIds;
        private readonly DateTime[] _simDates;
        private int[] _assetIndices;
        private int _nPathVectorBlocks;

        private IAssetFxModel _vanillaModel;

        public IAssetFxModel VanillaModel
        {
            get => _vanillaModel;
            set =>  _vanillaModel = value;
        }

        public string[] AssetIds => _assetIds;
        public DateTime[] SimDates => _simDates;

        public FactorReturnPayoff(string[] assetIds, DateTime[] simDates)
        {
            _assetIds = assetIds;
            _simDates = simDates;
        }

        public bool IsComplete => _isComplete;

        public IAssetInstrument AssetInstrument { get; private set; }
        public Currency SimulationCcy { get; }

        public void Finish(IFeatureCollection collection)
        {
            var dims = collection.GetFeature<IPathMappingFeature>();

            _assetIndices = AssetIds1.Select(assetId=>dims.GetDimension(assetId)).ToArray();

            if (_assetIndices.Min() < 0)
            {
                var err = _assetIndices.Select((x, ix) => (x, ix)).Where(f => f.x < 0).Select(f => f.ix).ToList();
                var missingAssets = string.Join(",", err.Select(e => AssetIds1[e]));
                throw new Exception($"Assets {missingAssets} not found in MC engine");
            }

            var dates = collection.GetFeature<ITimeStepsFeature>();
            _dateIndexes = new int[_simDates.Length];
            for (var i = 0; i < _simDates.Length; i++)
            {
                _dateIndexes[i] = dates.GetDateIndex(_simDates[i]);
            }

            var engine = collection.GetFeature<IEngineFeature>();
            _nPathVectorBlocks = engine.RoundedNumberOfPaths / Vector<double>.Count;
            _results = new Vector<double>[_assetIndices.Length][][];
            _rawNumberOfPaths = engine.NumberOfPaths;

            _isComplete = true;
        }

        public void Process(IPathBlock block)
        {
            if (_subInstruments != null)
            {
                foreach (var ins in _subInstruments)
                {
                    ins.Process(block);
                }
                return;
            }

            var blockBaseIx = block.GlobalPathIndex;

            for (var a = 0; a < _assetIndices.Length; a++)
            {
                _results[a] = new Vector<double>[_nPathVectorBlocks][];

                for (var path = 0; path < block.NumberOfPaths; path += Vector<double>.Count)
                {
                    var resultIx = (blockBaseIx + path) / Vector<double>.Count;
                    _results[a][resultIx] = new Vector<double>[_dateIndexes.Length];
                    var steps = block.GetStepsForFactor(path, _assetIndices[a]);

                    for(var tIx = 0; tIx < _dateIndexes.Length; tIx++)
                    {
                        _results[a][resultIx][tIx] = steps[_dateIndexes[tIx]];
                    }
                }
            }
        }

        public void SetupFeatures(IFeatureCollection pathProcessFeaturesCollection)
        {
            var dates = pathProcessFeaturesCollection.GetFeature<ITimeStepsFeature>();
            dates.AddDates(_simDates);
        }

        public double[][][] ResultsByPath
        {
            get
            {
                var vecLen = Vector<double>.Count;
                var results = new double[_assetIndices.Length][][]; // [_dateIndexes.Length][_rawNumberOfPaths];
                for (var a = 0; a < _assetIndices.Length; a++)
                {
                    results[a] = new double[_dateIndexes.Length][];
                    for (var tix = 0; tix < _dateIndexes.Length; tix++)
                    {
                        results[a][tix] = new double[_rawNumberOfPaths];
                        for (var i = 0; i < _results.Length; i++)
                        {
                            for (var j = 0; j < vecLen; j++)
                            {
                                var c = i * vecLen + j;
                                if (c >= results.Length)
                                    break;
                                // _results[a][resultIx][tIx] = steps[_dateIndexes[tIx]];
                                results[a][tix][c] = _results[a][i][tix][j];
                            }
                        }
                    }
                }
                return results;
            }
        }

        public CashFlowSchedule ExpectedFlows(IAssetFxModel model) => null;

        public CashFlowSchedule[] ExpectedFlowsByPath(IAssetFxModel model) => null;
    }
}
