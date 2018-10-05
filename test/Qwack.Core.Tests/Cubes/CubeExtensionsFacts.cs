using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Qwack.Core.Cubes;
using System.Linq;

namespace Qwack.Core.Tests.Cubes
{
    public class CubeExtensionsFacts
    {
        private ICube GetSUT()
        {
            var x = new ResultCube();
            x.Initialize(new Dictionary<string, Type> { { "gwah", typeof(string) }, { "wwahh", typeof(int) } });
            x.AddRow(new object[] { "woooh", 6 }, 77.6);
            x.AddRow(new Dictionary<string, object> { { "gwah", "gloop" }, { "wwahh", 14 } }, 78.6);
            x.AddRow(new Dictionary<string, object> { { "wwahh", 83 } , { "gwah", "bah!" } }, 79.6);

            return x;
        }
    
        [Fact]
        public void CanCreateCube()
        {
            var x = GetSUT();
            Assert.True(Enumerable.SequenceEqual(new[] { "woooh", "gloop", "bah!" }, x.KeysForField<string>("gwah")));
        }

        [Fact]
        public void CanFilter()
        {
            var x = GetSUT();
            var filters = new List<KeyValuePair<string, object>>
            {
                new KeyValuePair<string, object>( "gwah", "bah!" ),
                new KeyValuePair<string, object>( "gwah", "gloop" ),
            };
            var z = x.Filter(filters);
            Assert.True(Enumerable.SequenceEqual(new[] { "gloop", "bah!" }, z.KeysForField<string>("gwah")));

            var filters2 = new Dictionary<string, object> { { "wwahh", 6 } };
            z = x.Filter(filters2);
            Assert.True(Enumerable.SequenceEqual(new[] { "woooh" }, z.KeysForField<string>("gwah")));
        }

        [Fact]
        public void CanPivot()
        {
            var x = GetSUT();
            x.AddRow(new Dictionary<string, object> { { "wwahh", 88 }, { "gwah", "bah!" } }, 80.6);

            var z = x.Pivot("gwah", AggregationAction.Sum);
            var rowValue = z.GetAllRows().Where(r => Convert.ToString(r.MetaData.First()) == "bah!").Single().Value;
            Assert.Equal(80.6 + 79.6, rowValue);
        }
    }
}
