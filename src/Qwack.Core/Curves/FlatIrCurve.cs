using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Math.Interpolation;
namespace Qwack.Core.Curves
{
    public class FlatIrCurve:IrCurve
    {
        public FlatIrCurve(double rate, Currency ccy, string name)
            : base(new[] { DateTime.Today }, new[] { rate }, DateTime.Today, name, Interpolator1DType.DummyPoint, ccy)
        {

        }
    }
}
