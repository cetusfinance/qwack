using System;
using System.Collections.Generic;
using System.Text;

namespace Qwack.Transport.Results
{
    public struct LinearRegressionResult
    {
        private readonly double _alpha;
        private readonly double _beta;
        private readonly double _r2;
        private readonly double _sse;

        public LinearRegressionResult(double Alpha, double Beta, double R2, double SSE = 0)
        {
            _alpha = Alpha;
            _beta = Beta;
            _r2 = R2;
            _sse = SSE;
        }

        public double Alpha => _alpha;
        public double Beta => _beta;
        public double R2 => _r2;
        public double SSE => _sse;
    }
}
