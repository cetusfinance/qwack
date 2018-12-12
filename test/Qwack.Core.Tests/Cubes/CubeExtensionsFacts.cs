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

        [Fact]
        public void CanSort()
        {
            var x = GetSUT();
            x.AddRow(new Dictionary<string, object> { { "wwahh", 88 }, { "gwah", "bah!" } }, 80.6);

            var z = x.Sort();
            var rowValues = z.GetAllRows();
            Assert.Equal(79.6, rowValues.First().Value);
        }

        [Fact]
        public void CanSort2()
        {
            var x = GetSUT();
            x.AddRow(new Dictionary<string, object> { { "wwahh", 88 }, { "gwah", "bah!" } }, 80.6);

            var z = x.Sort(new List<string> { "wwahh" });
            var rowValues = z.GetAllRows();
            Assert.Equal(77.6, rowValues.First().Value);
        }

        private ICube GetSUT2()
        {
            var x = new ResultCube();
            x.Initialize(new Dictionary<string, Type> { { "gwah", typeof(string) }, { "wwahh", typeof(DateTime) } });
            x.AddRow(new object[] { "woooh", DateTime.Today.AddDays(10) }, 77.6);
            x.AddRow(new Dictionary<string, object> { { "gwah", "gloop" }, { "wwahh", DateTime.Today.AddDays(20) } }, 78.6);
            x.AddRow(new Dictionary<string, object> { { "wwahh", DateTime.Today.AddDays(30) }, { "gwah", "bah!" } }, 79.6);

            return x;
        }

        [Fact]
        public void CanBucket()
        {
            var x = GetSUT2();

            var buckets = new Dictionary<DateTime, string>
            {
                {DateTime.Today.AddDays(25),"b1" },
                {DateTime.Today.AddDays(50),"b2" },
                {DateTime.Today.AddDays(100),"b3" },
            };

            Assert.Throws<Exception>(()=>x.BucketTimeAxis("glooop", "bucketz", buckets));

            var z = x.BucketTimeAxis("wwahh", "bucketz", buckets);
            var rowValues = z.GetAllRows();
            Assert.Equal("b1", rowValues[0].ToDictionary(z.DataTypes.Keys.ToArray())["bucketz"]);
            Assert.Equal("b1", rowValues[1].ToDictionary(z.DataTypes.Keys.ToArray())["bucketz"]);
            Assert.Equal("b2", rowValues[2].ToDictionary(z.DataTypes.Keys.ToArray())["bucketz"]);
        }
    }
}
