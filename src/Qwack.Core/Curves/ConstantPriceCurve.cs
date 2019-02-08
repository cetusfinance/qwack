using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Core.Basic;

namespace Qwack.Core.Curves
{
    public class ConstantPriceCurve:PriceCurve
    {
        public ConstantPriceCurve(double price, DateTime originDate, ICurrencyProvider ccyProvider)
            : base(originDate, new[] { originDate }, new[] { price }, PriceCurveType.Constant, ccyProvider)
        {

        }
    }
}
