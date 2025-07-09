using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Qwack.Core.Basic;
using Qwack.Core.Models;
using Qwack.Math.Extensions;
using Qwack.Paths.Features;
using static System.Math;

namespace Qwack.Paths.Processes
{
    public class SchwartzSmithTwoFactorModel : IPathProcess, IRequiresFinish
    {
        private readonly SchwartzSmithTwoFactorModelParameters _params;

        private readonly DateTime _expiryDate;
        private readonly DateTime _startDate;
        private readonly int _numberOfSteps;
        private readonly string _name;
        private readonly Dictionary<DateTime, double> _pastFixings;
        
        private int _factorIndex1;
        private int _factorIndex2;
        private int _factorIndexPath;

        private ITimeStepsFeature _timesteps;
        private bool _isComplete;

        private Vector<double>[] _fixings;

        //https://core.ac.uk/download/pdf/289952496.pdf

        public SchwartzSmithTwoFactorModel(SchwartzSmithTwoFactorModelParameters ssParams, DateTime startDate, DateTime expiryDate, int nTimeSteps, string name, Dictionary<DateTime, double> pastFixings = null)
        {
            _params = ssParams;
            _startDate = startDate;
            _expiryDate = expiryDate;
            _numberOfSteps = nTimeSteps == 0 ? 100 : nTimeSteps;
            _name = name;
            _pastFixings = pastFixings ?? (new Dictionary<DateTime, double>());
        }

        public bool IsComplete => _isComplete;

        public void Finish(IFeatureCollection collection)
        {
            if (!_timesteps.IsComplete)
            {
                return;
            }

            var dates = collection.GetFeature<ITimeStepsFeature>();
            var fixings = new List<Vector<double>>();
            for (var d = 0; d < dates.Dates.Length; d++)
            {
                var date = dates.Dates[d];
                if (date >= _startDate) break;
                try
                {
                    var vect = new Vector<double>(_pastFixings[date]);
                    fixings.Add(vect);
                }
                catch (Exception e) 
                { 
                }

            }
            _fixings = [.. fixings];

            _isComplete = true;
        }

        public void Process(IPathBlock block)
        {
            for (var path = 0; path < block.NumberOfPaths; path += Vector<double>.Count)
            {
                var x1 = new Vector<double>(_params.X1_0);
                var x2 = new Vector<double>(_params.X2_0);
                var previousStep = (x1 + x2).Exp(64);
                var steps1 = block.GetStepsForFactor(path, _factorIndex1);
                var steps2 = block.GetStepsForFactor(path, _factorIndex2);
                var stepsOut = block.GetStepsForFactor(path, _factorIndexPath);

                var c = _fixings.Length;
                _fixings.AsSpan().CopyTo(stepsOut);
                stepsOut[c] = previousStep;

                var muVec = new Vector<double>(_params.Mu_Rn);
                var sigma1Vec = new Vector<double>(_params.Sigma_1);
                var sigma2Vec = new Vector<double>(_params.Sigma_2);
                var kappa2Vec = new Vector<double>(_params.Kappa_2);
                var lambda2Vec = new Vector<double>(_params.Lambda_2);
                for (var step = c + 1; step < block.NumberOfSteps; step++)
                {
                    var W1 = steps1[step];
                    var W2 = steps2[step];
                    var dt = new Vector<double>(_timesteps.TimeSteps[step]);
                    var dtRoot = new Vector<double>(_timesteps.TimeStepsSqrt[step]);
                    var dx1 = muVec * dt + sigma1Vec * dtRoot * W1;
                    var dx2 = - (lambda2Vec + (kappa2Vec * x2)) * dt + sigma2Vec * dtRoot * W2;
                    x1 += dx1;
                    x2 += dx2;
                    previousStep = (x1 + x2).Exp(64);
                    stepsOut[step] = previousStep;
                }
            }
        }

        public void SetupFeatures(IFeatureCollection pathProcessFeaturesCollection)
        {
            var mappingFeature = pathProcessFeaturesCollection.GetFeature<IPathMappingFeature>();
            _factorIndex1 = mappingFeature.AddDimension(_name + "~1");
            _factorIndex2 = mappingFeature.AddDimension(_name + "~2");
            _factorIndexPath = mappingFeature.AddDimension(_name);

            _timesteps = pathProcessFeaturesCollection.GetFeature<ITimeStepsFeature>();
            _timesteps.AddDate(_startDate);
            //_timesteps.AddDates(_pastFixings.Keys.Where(x => x < _startDate));

            var periodSize = (_expiryDate - _startDate).TotalDays;
            var stepSizeLinear = periodSize / _numberOfSteps;
            var simDates = new List<DateTime>();

            for (double i = 0; i < _numberOfSteps; i++)
            {
                var linStep = i * 1.0 / _numberOfSteps;
                var coshStep = (Cosh(linStep) - 1) / (Cosh(1) - 1);
                var stepDays = coshStep * periodSize;
                simDates.Add(_startDate.AddDays(stepDays));
            }
            _timesteps.AddDates(simDates.Distinct());
        }
    }
}

