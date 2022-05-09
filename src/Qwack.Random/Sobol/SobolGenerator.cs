using System.Threading.Tasks;
using static System.Math;

namespace Qwack.Random.Sobol
{
    public class SobolGenerator
    {
        public int Dimensions { get; set; }
        private double[] _points;
        private uint[] _c;
        private uint _l;
        private uint _n;
        private readonly SobolDirectionNumbers _directionNumbers;

        public SobolGenerator(SobolDirectionNumbers directionNumbers) => _directionNumbers = directionNumbers;

        public void GetPathsRaw(int numberOfPaths, double[] memory)
        {
            _n = (uint)numberOfPaths + 1;
            var D = (uint)Dimensions;

            //Max number of bits needed
            _l = (uint)Ceiling(Log(_n) / Log(2.0));

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

            // POINTS[i][j] = the jth component of the ith point
            //                with i indexed from 0 to N-1 and j indexed from 0 to D-1
            _points = memory;

            // ----- Compute the first dimension -----

            // Compute direction numbers V[1] to V[L], scaled by pow(2,32)
            var V = new uint[_l + 1];
            for (var i = 1; i <= _l; i++)
            {
                V[i] = (uint)(1 << (32 - i)); // all m's = 1
            }

            // Evalulate X[0] to X[N-1], scaled by pow(2,32)
            var X = new uint[_n];
            X[0] = 0;
            for (var i = 1; i <= _n - 1; i++)
            {
                X[i] = X[i - 1] ^ V[_c[i - 1]];
                if (i != 0)
                {
                    _points[GetIdx(i - 1, 0)] = X[i] / Pow(2.0, 32); // *** the actual points
                    //     ^ 0 for first dimension
                }
            }

            var tasks = new Task[D - 1];
            // ----- Compute the remaining dimensions -----
            for (var j = 1; j <= D - 1; j++)
            {
                tasks[j - 1] = new Task(o => GenerateDimension(o), j);
                tasks[j - 1].Start();
            }
            Task.WaitAll(tasks);
        }

        private int GetIdx(int path, int dimension) => path * Dimensions + dimension;

        private void GenerateDimension(object dm)
        {
            var dimension = (int)dm;
            var di = _directionNumbers.GetInfoForDimension(dimension + 1);
            // Read in parameters  
            //var d = (uint)di.Dimension;
            var s = di.S;
            var a = di.A;
            var m = new uint[s + 1];
            for (uint i = 1; i <= s; i++)
            {
                m[i] = di.DirectionNumbers[i - 1];
            }

            // Compute direction numbers V[1] to V[L], scaled by pow(2,32)
            var V1 = new uint[_l + 1];
            if (_l <= s)
            {
                for (var i = 1; i <= _l; i++)
                {
                    V1[i] = m[i] << (32 - i);
                }
            }
            else
            {
                for (var i = 1; i <= s; i++)
                {
                    V1[i] = m[i] << (32 - i);
                }
                for (var i = s + 1; i <= _l; i++)
                {
                    V1[i] = V1[i - s] ^ (V1[i - s] >> (int)s);
                    for (uint k = 1; k <= s - 1; k++)
                        V1[i] ^= (((a >> (int)(s - 1 - k)) & 1) * V1[i - k]);
                }
            }

            // Evalulate X[0] to X[N-1], scaled by pow(2,32)
            var X1 = new uint[_n];
            X1[0] = 0;
            for (var i = 1; i <= _n - 1; i++)
            {
                X1[i] = X1[i - 1] ^ V1[_c[i - 1]];
                _points[GetIdx(i - 1, dimension)] = (double)X1[i] / Pow(2.0, 32); // *** the actual points
                //     ^ j for dimension (j+1)
            }
        }
    }
}
