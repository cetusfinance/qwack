using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Qwack.Options
{
    /// <summary>
    /// List of pricing methods available for American options
    /// </summary>
    public enum AmericanPricingType
    {
        Binomial,
        Trinomial
    }
}
