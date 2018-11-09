using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Cubes;

namespace Qwack.Core.Models
{
    public interface IPvModel
    {
        ICube PV(Currency reportingCurrency);
    }
}
