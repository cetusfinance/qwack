using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Qwack.Core.Instruments.Funding
{
    /// <summary>
    /// Just a specific class for now, later we will need more features in this 
    /// so this gives us an easy way to do that without having to change interfaces
    /// </summary>
    public class FundingInstrumentCollection:List<IFundingInstrument>
    {

    }
}
