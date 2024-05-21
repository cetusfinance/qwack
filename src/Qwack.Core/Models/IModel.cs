using System.Collections.Generic;
using Qwack.Core.Cubes;
using Qwack.Core.Instruments;
using Qwack.Transport.BasicTypes;

namespace Qwack.Core.Models
{
    public interface IModel
    {

        bool IsSetupForPortfolio(Portfolio portfolio);

        List<BaseMetric> SupportedBaseMetrics { get; }
        ICube GetMetric(BaseMetric metric, Portfolio portfolio);
        ICube PV(Portfolio portfolio);
    }
}
