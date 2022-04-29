using Qwack.Core.Basic;
using Qwack.Core.Cubes;
using Qwack.Core.Instruments;
using Qwack.Core.Models;

namespace Qwack.Models.Models
{
    public class PnLAttributor : IPnLAttributor
    {
        public ICube BasicAttribution(Portfolio portfolio, IAssetFxModel startModel, IAssetFxModel endModel, Currency ccy, ICurrencyProvider ccyProvider)
            => portfolio.BasicAttribution(startModel, endModel, ccy, ccyProvider);

        public ICube ExplainAttribution(Portfolio portfolio, IAssetFxModel startModel, IAssetFxModel endModel, ICube startingGreeks, Currency ccy, ICurrencyProvider ccyProvider)
            => portfolio.ExplainAttribution(startModel, endModel, ccy, startingGreeks, ccyProvider);

        public ICube ExplainAttribution(Portfolio portfolioStart, Portfolio portfolioEnd, IAssetFxModel startModel, IAssetFxModel endModel, Currency ccy, ICurrencyProvider ccyProvider, bool cashOnDayAlreadyPaid = false)
            => portfolioStart.ExplainAttribution(portfolioEnd, startModel, endModel, ccy, ccyProvider, cashOnDayAlreadyPaid);

        public ICube ExplainAttributionInLineGreeks(Portfolio portfolio, IAssetFxModel startModel, IAssetFxModel endModel, Currency ccy, ICurrencyProvider ccyProvider, bool cashOnDayAlreadyPaid = false)
            => portfolio.ExplainAttributionInLineGreeks(startModel, endModel, ccy, ccyProvider, cashOnDayAlreadyPaid);
    }
}
