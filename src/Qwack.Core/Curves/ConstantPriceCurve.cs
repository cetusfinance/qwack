using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Transport.BasicTypes;

namespace Qwack.Core.Curves
{
    public class ConstantPriceCurve:BasicPriceCurve
    {

        public ConstantPriceCurve(double price, DateTime originDate, ICurrencyProvider ccyProvider)
            : base(originDate, new[] { originDate }, new[] { price }, PriceCurveType.Constant, ccyProvider) => Price = price;

        public double Price { get; }
    }
}
