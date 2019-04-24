using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Qwack.Core.Models;
using Qwack.Excel.Curves;
using Qwack.Excel.Utils;
using Qwack.Models;
using Xunit;
using static ExcelDna.Integration.ExcelMissing;

namespace Qwack.Excel.Tests.Models
{
    public class ModelFunctionsFacts
    {
        [Fact]
        public void CreateMcSettingsFact()
        {
            Assert.Equal("Could not find currency werp in cache",
                ModelFunctions.CreateMcSettings(null, 1, 1, null, null, null, "werp", null, false, false, null, null, null, null, null, null, null, null, null, false));

            Assert.Equal("Could not parse random generator name - eeek",
              ModelFunctions.CreateMcSettings(null, 1, 1, "eeek", null, null, "ZAR", null, false, false, null, null, null, null, null, null, null, null, null, false));

            Assert.Equal("Could not parse portfolio regressor type - eeek",
              ModelFunctions.CreateMcSettings(null, 1, 1, "Sobol", null, "eeek", "ZAR", null, false, false, null, null, null, null, null, null, null, null, null, false));

            Assert.Equal("Could not parse metric - eeek",
              ModelFunctions.CreateMcSettings(null, 1, 1, "Sobol", null, "MultiLinear", "ZAR", null, false, false, null, null, null, null, null, "eeek", null, null, null, false));

            Assert.Equal("Could not parse model type - eeek",
             ModelFunctions.CreateMcSettings(null, 1, 1, "Sobol", null, "MultiLinear", "ZAR", "eeek", false, false, null, null, null, null, null, "PV", null, null, null, false));

            Assert.Equal("boom¬0",
             ModelFunctions.CreateMcSettings("boom", 1, 1, "Sobol", Value, "MultiLinear", "ZAR", "Black", false, false, Value, Value, Value, Value, Value, "PV", Value, Value, Value, false));
        }


        [Fact]
        public void CreateMcPrecursorFact()
        {
            ContainerStores.GetObjectCache<IAssetFxModel>().PutObject("van", new SessionItem<IAssetFxModel>() { Name = "van", Value = new AssetFxModel(DateTime.Today, null) });
            ContainerStores.GetObjectCache<McSettings>().PutObject("set", new SessionItem<McSettings>() { Name = "set", Value = new McSettings() });
            Assert.Equal("boom¬0", ModelFunctions.CreateMcModel("boom", "van", "set"));
        }
    }
}
