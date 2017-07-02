using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Qwack.Paths;

namespace Qwack.Random.MersenneTwister
{
    /// <summary>
    /// Reference was mt19937-64.c 
    /// by Takuji Nishimura and Makoto Matsumoto.
    /// MT19937-64 (2004/9/29 version)
    /// </summary>
    public class MersenneTwister64 : IPathProcess
    {
        private static uint _nN = 312;
        private uint MM = 156;
        private ulong Matrix_A = 0xB5026F5AA96619E9UL;
        private ulong UpperM = 0xFFFFFFFF80000000UL;
        private ulong LowerM = 0x7FFFFFFFUL;
        private ulong[] mt = new ulong[_nN];
        private uint mti;

        public MersenneTwister64()
            : this(5489UL)
        {
        }

        public MersenneTwister64(ulong seed)
        {
            mt[0] = seed;
            for (mti = 1; mti < _nN; mti++)
            {
                mt[mti] = (6364136223846793005UL * (mt[mti - 1] ^ (mt[mti - 1] >> 62)) + mti);
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
                mt[i] = (mt[i] ^ ((mt[i - 1] ^ (mt[i - 1] >> 62)) * 3935559000370003845UL)) + initkey[j] + j; /* non linear */
                i++;
                j++;
                if (i >= _nN) { mt[0] = mt[_nN - 1]; i = 1; }
                if (j >= key_length) j = 0;
            }
            for (k = _nN - 1; k != 0; k--)
            {
                mt[i] = (mt[i] ^ ((mt[i - 1] ^ (mt[i - 1] >> 62)) * 2862933555777941757UL)) - i; /* non linear */
                i++;
                if (i >= _nN) { mt[0] = mt[_nN - 1]; i = 1; }
            }

            mt[0] = 1UL << 63; /* MSB is 1; assuring non-zero initial array */
        }

        public bool UseNormalInverse { get; set; }
        public bool UseAnthithetic { get; set; }
        /* generates a random number on [0, 2^64-1]-interval */
        public ulong GenerateInteger()
        {
            uint i;
            ulong x;
            ulong[] mag01 = { 0UL, Matrix_A };

            if (mti >= _nN)
            { /* generate NN words at one time */
                for (i = 0; i < _nN - MM; i++)
                {
                    x = (mt[i] & UpperM) | (mt[i + 1] & LowerM);
                    mt[i] = mt[i + MM] ^ (x >> 1) ^ mag01[(uint)(x & 1U)];
                }
                for (; i < _nN - 1; i++)
                {
                    x = (mt[i] & UpperM) | (mt[i + 1] & LowerM);
                    mt[i] = mt[i + (MM - _nN)] ^ (x >> 1) ^ mag01[(uint)(x & 1UL)];
                }
                x = (mt[_nN - 1] & UpperM) | (mt[0] & LowerM);
                mt[_nN - 1] = mt[MM - 1] ^ (x >> 1) ^ mag01[(uint)(x & 1UL)];

                mti = 0;
            }

            x = mt[mti++];

            x ^= (x >> 29) & 0x5555555555555555U;
            x ^= (x << 17) & 0x71D67FFFEDA60000U;
            x ^= (x << 37) & 0xFFF7EEE000000000U;
            x ^= (x >> 43);

            return x;
        }

        /* generates a random number on [0,1]-real-interval */
        public double GenerateDouble() => (GenerateInteger() >> 11) * (1.0 / 9007199254740991.0);

        public unsafe void Process(PathBlock block)
        {
            if (!UseNormalInverse)
            {
                for (var i = 0; i < block.TotalBlockSize; i = i + (UseAnthithetic ? 2 : 1))
                {
                    block[i] = GenerateDouble();
                    if (UseAnthithetic)
                        block[i + 1] = 1.0 - block[i];
                }
            }
            else
            {
                for (var i = 0; i < block.TotalBlockSize; i = i + (UseAnthithetic ? 2 : 1))
                {
                    block[i] = Math.Statistics.NormInv(GenerateDouble());
                    if (UseAnthithetic)
                        block[i + 1] = -block[i];
                }
            }
        }

        public void SetupFeatures(FeatureCollection pathProcessFeaturesCollection)
        {

        }
    }
}
