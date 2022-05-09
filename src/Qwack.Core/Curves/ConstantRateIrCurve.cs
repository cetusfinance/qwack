using System;
using Qwack.Core.Basic;
using Qwack.Transport.BasicTypes;

namespace Qwack.Core.Curves
{
    public class ConstantRateIrCurve : IrCurve
    {
        public ConstantRateIrCurve(double rate, DateTime buildDate, string name, Currency ccy, string collateralSpec = null, RateType rateStorageType = RateType.Exponential)
            : base(new[] { buildDate }, new[] { rate }, buildDate, name, Interpolator1DType.DummyPoint, ccy, collateralSpec, rateStorageType)
        {
        }
    }
}
