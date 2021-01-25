using System;
using System.Collections.Generic;
using System.Text;

namespace Qwack.Core.Basic
{
    /// <summary>
    /// BestOf - most positive performance
    /// </summary>
    public enum BasketType
    {
        SingleAsset,
        EquallyWeightedBasket,
        WeightedBasket,
        BestOf,
        WorstOf,
        NthBestOf,
        NthWorstOf
    }
}
