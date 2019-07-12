using Qwack.Core.Basic;
using Qwack.Core.Cubes;
using Qwack.Core.Instruments;
using Qwack.Core.Models;

namespace Qwack.Models.Models
{
    public interface IPnLAttributor
    {
        ICube BasicAttribution(Portfolio portfolio, IAssetFxModel startModel, IAssetFxModel endModel, Currency ccy, ICurrencyProvider ccyProvider);
        ICube ExplainAttribution(Portfolio portfolio, IAssetFxModel startModel, IAssetFxModel endModel, ICube startingGreeks, Currency ccy, ICurrencyProvider ccyProvider);
        ICube ExplainAttribution(Portfolio portfolioStart, Portfolio portfolioEnd, IAssetFxModel startModel, IAssetFxModel endModel, Currency ccy, ICurrencyProvider ccyProvider, bool cashOnDayAlreadyPaid = false);
        ICube ExplainAttributionInLineGreeks(Portfolio portfolio, IAssetFxModel startModel, IAssetFxModel endModel, Currency ccy, ICurrencyProvider ccyProvider, bool cashOnDayAlreadyPaid=false);
    }
}
