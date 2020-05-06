using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Transport.BasicTypes;

namespace Qwack.Transport.TransportObjects.Interpolators
{
    public class TO_Interpolator1d
    {
        public double[] Xs { get; set; }
        public double[] Ys { get; set; }
        public Interpolator1DType Type { get; set; }
        public bool IsSorted { get; set; }
        public bool NoCopy { get; set; }
    }
}
