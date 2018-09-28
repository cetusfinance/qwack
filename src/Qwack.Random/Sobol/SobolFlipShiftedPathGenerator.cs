using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using Qwack.Core.Models;
using Qwack.Paths;
using Qwack.Paths.Features;
using static System.Math;

namespace Qwack.Random.Sobol
{
    public class SobolFlipShiftedPathGenerator : IPathProcess, IRequiresFinish
    {
        private int _timesteps;
        private int _factors;
        private SobolDirectionNumbers _directionNumbers;
        private uint _bitsRequired;
        private uint[][] _v;
        private uint _n;
        private uint[] _c;
        private int _numberOfPaths;
        private readonly bool _useNormalInv;
        private double[] _dimensionShifts;

        public SobolFlipShiftedPathGenerator(bool useNormalInv, SobolDirectionNumbers directionNumbers)
        {
            _useNormalInv = useNormalInv;
            _directionNumbers = directionNumbers;
        }

        public bool IsComplete => true;
        
        public void Finish(IFeatureCollection collection)
        {
            _numberOfPaths = collection.GetFeature<IEngineFeature>().NumberOfPaths + 1;
            _factors = collection.GetFeature<IPathMappingFeature>().NumberOfDimensions;
            _timesteps = collection.GetFeature<ITimeStepsFeature>().TimeStepCount;
            Init1stDimension();
            SetupShifts();
        }

        private double ShiftNumber(double originalNumber, int dimension)
        {
            originalNumber = originalNumber + _dimensionShifts[dimension];
            if (originalNumber >= 1.0)
                return originalNumber - 1.0;
            return originalNumber;
        }

        private uint CreatePoint(int path, int dimension)
        {
            uint returnValue = 0;
            var uPath = (uint)path + 1;

            for (var bit = 0; bit < _bitsRequired; bit++)
            {
                var bitMask = (uint)1 << bit;

                var currentBit = uPath & bitMask;
                currentBit = currentBit >> bit;

                returnValue = returnValue ^ (_v[dimension][bit + 1] * currentBit);

            }
            return returnValue;
        }

        private void Init1stDimension()
        {
            _n = (uint)_numberOfPaths + 1;
            var D = (uint)(_timesteps * _factors);

            //Max number of bits needed
            _bitsRequired = (uint)Ceiling(Log((double)_n) / Log(2.0));

            //Set all of the C numbers
            _c = new uint[_n];

            _c[0] = 1;
            for (uint i = 1; i <= _n - 1; i++)
            {
                _c[i] = 1;
                var value = i;
                while ((value & 1) != 0)
                {
                    value >>= 1;
                    _c[i]++;
                }
            }

            //Direction numbers
            _v = new uint[D][];
            for (var i = 0; i < D; i++)
            {
                _v[i] = new uint[_bitsRequired + 1];
            }

            //Set directions for first dimension

            for (var i = 1; i <= _bitsRequired; i++)
            {
                _v[0][i] = (uint)(1 << (32 - i)); // all m's = 1
            }

            //Compute other dimensions numbers 
            for (var j = 1; j < D; j++)
            {
                GenerateDimensionNumbers(j);
            }
        }

        private void GenerateDimensionNumbers(int dimension)
        {
            var di = _directionNumbers.GetInfoForDimension(dimension + 1);

            // Read in parameters  
            var d = (uint)di.Dimension;
            var s = di.S;
            var a = di.A;
            var m = new uint[s + 1];
            for (uint i = 1; i <= s; i++)
            {
                m[i] = di.DirectionNumbers[i - 1];
            }

            // Compute direction numbers V[1] to V[L], scaled by pow(2,32)
            if (_bitsRequired  <= s)
            {
                for (var i = 1; i <= _bitsRequired; i++) _v[dimension][i] = m[i] << (32 - i);
            }
            else
            {
                for (var i = 1; i <= s; i++) _v[dimension][i] = m[i] << (32 - i);
                for (var i = s + 1; i <= _bitsRequired; i++)
                {
                    _v[dimension][i] = _v[dimension][i - s] ^ (_v[dimension][i - s] >> (int)s);
                    for (uint k = 1; k <= s - 1; k++)
                    {
                        _v[dimension][i] ^= (((a >> (int)(s - 1 - k)) & 1) * _v[dimension][i - k]);
                    }
                }
            }
        }

        public void SetupShifts()
        {
            _dimensionShifts = new double[_timesteps * _factors];

            for (var i = 1; i <= _dimensionShifts.Length; i++)
            {
                _dimensionShifts[i - 1] = (i - 1.0) / i;
            }
        }

        public void Process(IPathBlock block)
        {
            var index = 0;
            for(var path = 0; path < block.NumberOfPaths;path+=Vector<double>.Count)
            {
                for(var factor = 0; factor < block.Factors;factor++)
                {
                    for(var step = 0; step < block.NumberOfSteps;step++)
                    {
                        var dim = step * block.Factors + factor;

                        for (var i = 0; i < Vector<double>.Count; i++)
                        {
                            var pathid = path + i + block.GlobalPathIndex;
                            var point =CreatePoint(pathid, dim) / Pow(2.0,32.0);
                            //point = ShiftNumber(point,dim);
                            if(_useNormalInv)
                            {
                                point = Math.Statistics.NormInv(point);
                            }
                            block.RawData[index] = point;
                            index++;
                        }
                    }
                }
            }
            Debug.Assert(block.RawData.Length == index);
        }

        public void SetupFeatures(IFeatureCollection pathProcessFeaturesCollection)
        {
        }
    }
}
