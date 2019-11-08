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
        public void CreateMcPrecursorFact()
        {
            ContainerStores.GetObjectCache<IAssetFxModel>().PutObject("van", new SessionItem<IAssetFxModel>() { Name = "van", Value = new AssetFxModel(DateTime.Today, null) });
            ContainerStores.GetObjectCache<McSettings>().PutObject("set", new SessionItem<McSettings>() { Name = "set", Value = new McSettings() });
            Assert.Equal("boom¬0", ModelFunctions.CreateMcModel("boom", "van", "set"));
        }
    }
}
