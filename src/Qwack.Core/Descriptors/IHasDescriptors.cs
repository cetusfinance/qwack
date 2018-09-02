using System;
using System.Collections.Generic;
using System.Text;

namespace Qwack.Core.Descriptors
{
    public interface IHasDescriptors
    {
        List<MarketDataDescriptor> Descriptors { get; }
        List<MarketDataDescriptor> Dependencies { get; }
    }
}
