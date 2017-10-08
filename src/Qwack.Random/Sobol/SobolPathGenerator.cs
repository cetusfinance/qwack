using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using Qwack.Math;
using Qwack.Paths;
using Qwack.Paths.Features;
using static System.Math;
namespace Qwack.Random.Sobol
{
    public class SobolPathGenerator : IPathProcess, IRequiresFinish
    {
        private static double s_convertToDoubleConstant = System.Math.Pow(2.0, -32.0);

        private int _numberOfDimensions;
        private SobolDirectionNumbers _directionNumbers;
        private int _seed;
        private uint _bitsRequired;
        private uint[][] _v;
        private int _numberOfPaths;

        public SobolPathGenerator(SobolDirectionNumbers directionNumbers, int seed)
        {
            _seed = seed;
            _directionNumbers = directionNumbers;
        }

        public bool IsComplete => true;
        public bool UseNormalInverse { get; set; }

        public void Finish(FeatureCollection collection)
        {
            _numberOfPaths = collection.GetFeature<IEngineFeature>().NumberOfPaths;
            _numberOfDimensions = collection.GetFeature<IPathMappingFeature>().NumberOfDimensions;
            _numberOfDimensions *= collection.GetFeature<ITimeStepsFeature>().TimeStepCount;
            InitDimensions();
        }

        //public void Process(PathBlock block)
        //{
        //    for (var s = 0; s < block.NumberOfSteps; s++)
        //    {
        //        for (var f = 0; f < block.Factors; f++)
        //        {
        //            for (var p = 0; p < block.NumberOfPaths; p += Vector<double>.Count)
        //            {
        //                var masterGreyCode = (uint)(block.GlobalPathIndex + 1 + p);
        //                masterGreyCode = masterGreyCode ^ (masterGreyCode >> 1);
        //                var currentDim = s * block.Factors + f;
        //                var gCode = masterGreyCode;
        //                uint x = 0;

        //                //First path
        //                for (var i = 0; i < _bitsRequired; i++)
        //                {
        //                    var mask = (uint)-(gCode & 1);
        //                    x ^= mask & _v[currentDim + _seed][i];
        //                    gCode = gCode >> 1;
        //                }
        //                gCode = x;

        //                var temp = new double[Vector<double>.Count];
        //                temp[0] = ConvertRandToDouble(x);
        //                for (var i = 1; i < Vector<double>.Count; i++)
        //                {
        //                    gCode = gCode ^ _v[currentDim + _seed][BitShifting.FindFirstSet(~(uint)(i + p)) - 1];
        //                    temp[i] = ConvertRandToDouble(gCode);
        //                }
        //                var path = block.GetStepsForFactor(p, f);
        //                path[s] = new Vector<double>(temp);
        //            }
        //        }
        //    }
        //}

        public void Process(PathBlock block)
        {
            for(var s = 0; s < block.NumberOfSteps;s++)
            {
                for(var f = 0; f < block.Factors;f++)
                {
                    var dim = f * block.NumberOfSteps + s;
                    for(var p = 0; p < block.NumberOfPaths;p += Vector<double>.Count)
                    {
                        var temp = new double[Vector<double>.Count];
                        for(var i = 0; i < Vector<double>.Count;i++)
                        {
                            var path = i + p + block.GlobalPathIndex;
                            temp[i] = GetValueViaGreyCode(path, dim);
                        }
                        var pathSpan = block.GetStepsForFactor(p, f);
                        pathSpan[s] = new Vector<double>(temp);
                    }
                }
            }
        }

        public void Finish()
        {
        }

        public double GetValueViaGreyCode(int path, int dimension)
        {
            var nPath = (uint)path + 1;
            var greyMasterCode = nPath ^ (nPath >> 1);
            uint x = 0;
            var gCode = greyMasterCode;
            for (var i = 0; i < _bitsRequired; i++)
            {
                var mask = (uint)-(gCode & 1);
                x ^= mask & _v[dimension + _seed][i];
                gCode = gCode >> 1;
            }
            return ConvertRandToDouble(x);
        }
        

        //public void Process(PathBlock block)
        //{
        //    var firstPath = block.GetEntirePath(0);

        //    var nPath = (uint)block.GlobalPathIndex + 1;
        //    var greyMasterCode = nPath ^ (nPath >> 1);
        //    var codes = new uint[_numberOfDimensions];
        //    for (var d = 0; d < codes.Length; d++)
        //    {
        //        uint x = 0;
        //        var gCode = greyMasterCode;

        //        for (var i = 0; i < _bitsRequired; i++)
        //        {
        //            var mask = (uint)-(gCode & 1);
        //            x ^= mask & _v[d + _seed][i];
        //            gCode = gCode >> 1;
        //        }
        //        codes[d] = x;
        //        var val = ConvertRandToDouble(x);
        //        Debug.Assert(!double.IsNaN(val));
        //        firstPath[d] = val;
        //    }

        //    for (var p = 1; p < block.NumberOfPaths; p++)
        //    {
        //        var currentPath = block.GetEntirePath(p);
        //        for (var d = 0; d < codes.Length; d++)
        //        {

        //        }
        //    }
        //}

        public void SetupFeatures(FeatureCollection pathProcessFeaturesCollection)
        {
        }

        private double ConvertRandToDouble(uint rand)
        {
            var val = rand * s_convertToDoubleConstant;
            if (UseNormalInverse)
            {
                val = Statistics.NormInv(val);
            }
            return val;
        }

        protected void InitDimensions()
        {

            var nDimensions = _numberOfDimensions + _seed;

            //Max number of bits needed
            _bitsRequired = (uint)Ceiling(Log(_numberOfPaths + 1.0) / Log(2.0));

            //Direction numbers
            _v = new uint[nDimensions][];
            for (var i = 0; i < (nDimensions); i++)
            {
                _v[i] = new uint[_bitsRequired];
            }

            for (var dim = 0; dim < nDimensions; dim++)
            {
                var v = _v[dim];

                //First dim special case
                if (dim == 0)
                {
                    for (var i = 0; i < _bitsRequired; i++)
                    {
                        v[i] = 1u << (31 - i);
                    }
                }
                else
                {
                    var di = _directionNumbers.GetInfoForDimension(dim + 1);
                    var degree = (int)di.S;
                    if (_bitsRequired < degree)
                    {
                        for (var i = 0; i < _bitsRequired; i++)
                        {
                            v[i] = di.DirectionNumbers[i] << (31 - i);
                        }
                    }
                    else
                    {
                        for (var i = 0; i < degree; i++)
                        {
                            v[i] = di.DirectionNumbers[i] << (31 - i);
                        }

                        // The remaining direction numbers are computed as described in
                        // the Bratley and Fox paper.
                        // v[i] = a[1]v[i-1] ^ a[2]v[i-2] ^ ... ^ a[v-1]v[i-d+1] ^ v[i-d] ^ v[i-d]/2^d

                        for (var i = degree; i < _bitsRequired; i++)
                        {
                            // First do the v[i-d] ^ v[i-d]/2^d part
                            v[i] = v[i - degree] ^ (v[i - degree] >> degree);

                            // Now do the a[1]v[i-1] ^ a[2]v[i-2] ^ ... part
                            // Note that the coefficients a[] are zero or one and for compactness in
                            // the input tables they are stored as bits of a single integer. To extract
                            // the relevant bit we use right shift and mask with 1.
                            // For example, for a 10 degree polynomial there are ten useful bits in a,
                            // so to get a[2] we need to right shift 7 times (to get the 8th bit into
                            // the LSB) and then mask with 1.
                            for (var j = 1; j < degree; j++)
                            {
                                v[i] ^= ((((uint)di.A >> (int)(degree - 1u - j)) & 1) * v[i - j]);
                            }

                        }
                    }
                }
            }
        }
    }
}
