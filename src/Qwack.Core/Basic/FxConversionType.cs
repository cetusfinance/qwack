using System;
using System.Collections.Generic;
using System.Text;

namespace Qwack.Core.Basic
{
    public enum FxConversionType
    {
        None=0,
        ConvertThenAverage=1,
        CTA=1,
        AverageThenConvert=2,
        ATC=2,
        SettleOtherCurrency=3,
        SOC=3
    }
}
