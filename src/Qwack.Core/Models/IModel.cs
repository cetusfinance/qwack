using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Cubes;
using Qwack.Core.Descriptors;
using Qwack.Core.Instruments;

namespace Qwack.Core.Models
{
    public interface IModel
    {         
        List<MarketDataDescriptor> GetRequirements(Portfolio portfolio);
        bool ProvideRequirements(Dictionary<MarketDataDescriptor, object> values);

        bool IsSetupForPortfolio(Portfolio portfolio);

        List<BaseMetric> SupportedBaseMetrics { get; }
        ICube GetMetric(BaseMetric metric, Portfolio portfolio);
        ICube PV(Portfolio portfolio);
    }
}
