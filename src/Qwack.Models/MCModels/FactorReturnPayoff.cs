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

        private double[][][] _results;
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

        public Dictionary<string, int> AssetIndices { get; private set; } = new Dictionary<string, int>();
        public Dictionary<DateTime, int> DateIndices { get; private set; } = new Dictionary<DateTime, int>();

        public bool IsComplete => _isComplete;

        public IAssetInstrument AssetInstrument { get; private set; }
        public Currency SimulationCcy { get; }

        public void Finish(IFeatureCollection collection)
        {
            var dims = collection.GetFeature<IPathMappingFeature>();

            _assetIndices = AssetIds.Select(assetId=>dims.GetDimension(assetId)).ToArray();

            if (_assetIndices.Min() < 0)
            {
                var err = _assetIndices.Select((x, ix) => (x, ix)).Where(f => f.x < 0).Select(f => f.ix).ToList();
                var missingAssets = string.Join(",", err.Select(e => AssetIds[e]));
                throw new Exception($"Assets {missingAssets} not found in MC engine");
            }

            for(var a = 0; a < AssetIds.Length; a++)
            {
                AssetIndices[AssetIds[a]] = _assetIndices[a];
            }

            var dates = collection.GetFeature<ITimeStepsFeature>();
            _dateIndexes = new int[_simDates.Length];
            for (var i = 0; i < _simDates.Length; i++)
            {
                _dateIndexes[i] = dates.GetDateIndex(_simDates[i]);
                DateIndices[_simDates[i]] = _dateIndexes[i];
            }

            var engine = collection.GetFeature<IEngineFeature>();
            _nPathVectorBlocks = engine.RoundedNumberOfPaths / Vector<double>.Count;
            _results = new double[_assetIndices.Length][][];
            _rawNumberOfPaths = engine.NumberOfPaths;

            _isComplete = true;
        }

        public void Process(IPathBlock block)
        {
            var blockBaseIx = block.GlobalPathIndex;
            var vecLen = Vector<double>.Count;

            for (var a = 0; a < _assetIndices.Length; a++)
            {
                if(_results[a] == null) 
                    _results[a] = new double[_rawNumberOfPaths][];

                for (var path = 0; path < block.NumberOfPaths; path += Vector<double>.Count)
                {
                    var resultIx = (blockBaseIx + path) / Vector<double>.Count;
                    var steps = block.GetStepsForFactor(path, _assetIndices[a]);
                    for (var j = 0; j < vecLen; j++)
                    {
                        var c = resultIx * vecLen + j;
                        if(_results[a][c]==null)
                            _results[a][c] = new double[_dateIndexes.Length];

                        if (c >= _results[a].Length)
                            break;
         
                        for (var tIx = 0; tIx < _dateIndexes.Length; tIx++)
                        {
                            //_results[a][resultIx][tIx] = steps[_dateIndexes[tIx]][1];
                            _results[a][c][tIx] = steps[_dateIndexes[tIx]][j];
                        }
                    }
                }
            }
        }

        public void SetupFeatures(IFeatureCollection pathProcessFeaturesCollection)
        {
            var dates = pathProcessFeaturesCollection.GetFeature<ITimeStepsFeature>();
            dates.AddDates(_simDates);
        }

        /// <summary>
        /// [AssetIx][PathIx][TimeIx]
        /// </summary>
        public double[][][] ResultsByPath => _results;        

        public CashFlowSchedule ExpectedFlows(IAssetFxModel model) => null;

        public CashFlowSchedule[] ExpectedFlowsByPath(IAssetFxModel model) => null;
    }
}
