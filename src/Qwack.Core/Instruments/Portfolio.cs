using System;
using System.Collections.Generic;
using System.Text;

namespace Qwack.Core.Instruments
{
    public class Portfolio : IInstrument
    {
        public List<IInstrument> Instruments { get; set; }
    }
}
