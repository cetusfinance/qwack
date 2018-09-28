using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Qwack.Core.Basic
{
    public interface ICurrencyProvider
    {
        Currency this[string ccy] { get; }
    }
}
