using System;
using System.Collections.Generic;
using System.Text;

namespace Qwack.Core.Descriptors
{
    public abstract class MarketDataDescriptor
    {
        public DateTime ValDate { get; set; }
        public string Name { get; set; }
        public string SetName { get; set; }

        public List<MarketDataDescriptor> Dependencies { get; } = new List<MarketDataDescriptor>();
    }
}
