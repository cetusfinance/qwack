using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Transport.BasicTypes;

namespace Qwack.Transport.TransportObjects
{
    public class TO_Interpolator2d_Square
    {
        public double[] Xs { get; set; }
        public double[] Ys { get; set; }
        public double[,] Zs { get; set; }
        public Interpolator2DType Type { get; set; }
    }
}
