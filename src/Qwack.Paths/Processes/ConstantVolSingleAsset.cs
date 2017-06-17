using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Qwack.Paths.Features;
using static System.Math;
namespace Qwack.Paths.Processes
{
    public class ConstantVolSingleAsset : IPathProcess
    {
        private DateTime _startDate;
        private DateTime _expiry;
        private double _vol;
        private double _scaledVol;
        private double _spot;
        private double _drift;
        private int _numberOfSteps;
        private string _name;
        private ITimeStepsFeature _timesteps;

        public ConstantVolSingleAsset(DateTime startDate, DateTime expiry, double vol, double spot, double drift, int numberOfSteps, string name)
        {
            _startDate = startDate;
            _expiry = expiry;
            _vol = vol;
            _spot = spot;
            _drift = drift;
            _numberOfSteps = numberOfSteps;
            _name = name;
            _scaledVol = _vol / Sqrt(365.0);
        }

        public void Process(PathBlock block)
        {
            var currentIndex = 0 * block.NumberOfPaths * block.NumberOfSteps;
            for (var path = 0; path < block.NumberOfPaths; path += Vector<double>.Count)
            {
                //This should be set to the spot price here
                var previousStep = new Vector<double>(_spot);
                for (var step = 0; step < block.NumberOfSteps; step++)
                {
                    ref Vector<double> currentValue = ref block.ReadVectorByRef(currentIndex);
                    var drift = _drift * _timesteps.TimeSteps[step] * previousStep;
                    var delta = _scaledVol * currentValue;
                    currentValue = (previousStep + drift + delta);
                    //block.WriteVector(currentIndex, currentValue);
                    previousStep = currentValue;
                    currentIndex += Vector<double>.Count;
                }
            }
        }

        public void SetupFeatures(FeatureCollection pathProcessFeaturesCollection)
        {
            var mappingFeature = pathProcessFeaturesCollection.GetFeature<IPathMappingFeature>();
            mappingFeature.AddDimension(_name);

            _timesteps = pathProcessFeaturesCollection.GetFeature<ITimeStepsFeature>();
            var stepSize = (_expiry - _startDate).TotalDays;
            for(var i = 0; i < _numberOfSteps;i++)
            {
                _timesteps.AddDate(_startDate.AddDays(i * stepSize));
            }
        }
    }
}
