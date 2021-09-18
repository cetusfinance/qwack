using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Qwack.Core.Models;
using Qwack.Paths;
using Qwack.Paths.Features;

namespace Qwack.Random.MersenneTwister
{
    /// <summary>
    /// Reference was mt19937-64.c 
    /// by Takuji Nishimura and Makoto Matsumoto.
    /// MT19937-64 (2004/9/29 version)
    /// </summary>
    public class MersenneTwister64 : IPathProcess, IRunSingleThreaded
    {
        private static readonly uint _nN = 312;
        private const uint _mm = 156;
        private const ulong _matrix_A = 0xB5026F5AA96619E9UL;
        private const ulong _upperM = 0xFFFFFFFF80000000UL;
        private const ulong _lowerM = 0x7FFFFFFFUL;
        private readonly ulong[] _mt = new ulong[_nN];
        private uint _mti;

        public MersenneTwister64()
            : this(5489UL)
        {
        }

        public MersenneTwister64(ulong seed)
        {
            _mt[0] = seed;
            for (_mti = 1; _mti < _nN; _mti++)
            {
                _mt[_mti] = (6364136223846793005UL * (_mt[_mti - 1] ^ (_mt[_mti - 1] >> 62)) + _mti);
            }
        }

        public MersenneTwister64(ulong[] initkey, uint key_length)
            : this(19650218UL)
        {
            ulong i, j, k;
            i = 1; j = 0;
            k = (_nN > key_length ? _nN : key_length);
            for (; k != 0; k--)
            {
                _mt[i] = (_mt[i] ^ ((_mt[i - 1] ^ (_mt[i - 1] >> 62)) * 3935559000370003845UL)) + initkey[j] + j; /* non linear */
                i++;
                j++;
                if (i >= _nN) { _mt[0] = _mt[_nN - 1]; i = 1; }
                if (j >= key_length) j = 0;
            }
            for (k = _nN - 1; k != 0; k--)
            {
                _mt[i] = (_mt[i] ^ ((_mt[i - 1] ^ (_mt[i - 1] >> 62)) * 2862933555777941757UL)) - i; /* non linear */
                i++;
                if (i >= _nN) { _mt[0] = _mt[_nN - 1]; i = 1; }
            }

            _mt[0] = 1UL << 63; /* MSB is 1; assuring non-zero initial array */
        }

        public bool UseNormalInverse { get; set; }
        public bool UseAnthithetic { get; set; }
        /* generates a random number on [0, 2^64-1]-interval */
        public ulong GenerateInteger()
        {
            uint i;
            ulong x;
            ulong[] mag01 = { 0UL, _matrix_A };

            if (_mti >= _nN)
            { /* generate NN words at one time */
                for (i = 0; i < _nN - _mm; i++)
                {
                    x = (_mt[i] & _upperM) | (_mt[i + 1] & _lowerM);
                    _mt[i] = _mt[i + _mm] ^ (x >> 1) ^ mag01[(uint)(x & 1U)];
                }
                for (; i < _nN - 1; i++)
                {
                    x = (_mt[i] & _upperM) | (_mt[i + 1] & _lowerM);
                    _mt[i] = _mt[i + (_mm - _nN)] ^ (x >> 1) ^ mag01[(uint)(x & 1UL)];
                }
                x = (_mt[_nN - 1] & _upperM) | (_mt[0] & _lowerM);
                _mt[_nN - 1] = _mt[_mm - 1] ^ (x >> 1) ^ mag01[(uint)(x & 1UL)];

                _mti = 0;
            }

            x = _mt[_mti++];

            x ^= (x >> 29) & 0x5555555555555555U;
            x ^= (x << 17) & 0x71D67FFFEDA60000U;
            x ^= (x << 37) & 0xFFF7EEE000000000U;
            x ^= (x >> 43);

            return x;
        }

        /* generates a random number on [0,1]-real-interval */
        public double GenerateDouble() => (GenerateInteger() >> 11) * (1.0 / 9007199254740991.0);

        public unsafe void Process(IPathBlock block)
        {
            if (!UseNormalInverse)
            {
                for (var i = 0; i < block.TotalBlockSize; i += (UseAnthithetic ? 2 : 1))
                {
                    block[i] = GenerateDouble();
                    if (UseAnthithetic)
                        block[i + 1] = 1.0 - block[i];
                }
            }
            else
            {
                for (var i = 0; i < block.TotalBlockSize; i += (UseAnthithetic ? 2 : 1))
                {
                    block[i] = Math.Statistics.NormInv(GenerateDouble());
                    if (UseAnthithetic)
                        block[i + 1] = -block[i];
                }
            }
        }

        public void SetupFeatures(IFeatureCollection pathProcessFeaturesCollection)
        {

        }
    }
}
