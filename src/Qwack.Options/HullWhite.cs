using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Math;

namespace Qwack.Options
{
    public class HullWhite
    {
        public double SigmaR { get; private set; }
        public double Alpha { get; private set; }
        public IInterpolator1D Theta { get; private set; }

        //private double A(double t, double T)
        //{

        //}
    }
}
