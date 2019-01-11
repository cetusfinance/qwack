using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Instruments;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Futures;

namespace Qwack.Models.MCModels
{
    public class AssetFXMCModelPercursor
    {
        public IAssetFxModel AssetFxModel { get; set; }
        public McSettings McSettings { get; set; }

        public ICurrencyProvider CcyProvider { get; set; }
        public ICalendarProvider CalendarProvider { get; set; }
        public IFutureSettingsProvider FutProvider { get; set; }

        public AssetFxMCModel ToModel(Portfolio portfolio) => new AssetFxMCModel(AssetFxModel.BuildDate, portfolio, AssetFxModel, McSettings, CcyProvider, FutProvider, CalendarProvider);
    }
}
