using System;
using System.Collections.Generic;
using System.Text;

namespace Qwack.Core.Basic
{
    public enum FxConversionType
    {
        None,
        ConvertThenAverage,
        AverageThenConvert,
        SettleOtherCurrency
    }
}
