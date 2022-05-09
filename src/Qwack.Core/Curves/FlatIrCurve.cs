using System;
using Qwack.Core.Basic;
using Qwack.Transport.BasicTypes;

namespace Qwack.Core.Curves
{
    public class FlatIrCurve : IrCurve
    {
        public FlatIrCurve(double rate, Currency ccy, string name)
            : base(new[] { DateTime.MaxValue }, new[] { rate }, DateTime.Today, name, Interpolator1DType.DummyPoint, ccy)
        {

        }
    }
}
