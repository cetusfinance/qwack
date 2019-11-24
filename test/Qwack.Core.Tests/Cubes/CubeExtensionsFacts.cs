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
            x.AddRow(new Dictionary<string, object> { { "wwahh", 83 }, { "gwah", "bah!" } }, 79.6);

            return x;
        }

        [Fact]
        public void CanCreateCube()
        {
            var x = GetSUT();
            Assert.True(Enumerable.SequenceEqual(new[] { "woooh", "gloop", "bah!" }, x.KeysForField<string>("gwah")));
        }

        [Fact]
        public void ToDictionary()
        {
            var x = GetSUT();
            Assert.Throws<Exception>(() => x.ToDictionary("ooop"));
            var d = x.ToDictionary("gwah");

            Assert.Equal(79.6, d["bah!"].Single().Value);
        }

        [Fact]
        public void KeysForField()
        {
            var x = GetSUT();
            Assert.Throws<Exception>(() => x.KeysForField<string>("ooop"));
            Assert.Throws<Exception>(() => x.KeysForField<string>("wwahh"));
            Assert.Throws<Exception>(() => x.KeysForField("ooop"));

            var k = x.KeysForField<int>("wwahh");
            Assert.Contains(14, k);

            var ko = x.KeysForField("wwahh");
            Assert.Contains(14, ko);

            var kos = x.KeysForField("gwah");
            Assert.Contains("gloop", kos);
        }

        [Fact]
        public void CanMultiply()
        {
            var x = GetSUT();
            var xL = x.ScalarMultiply(10.0);

            Assert.Equal(796, xL.ToDictionary("gwah")["bah!"].Single().Value);
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

            var filters3 = new Dictionary<string, object> { { "ooop", 6 } };
            Assert.Throws<Exception>(() => x.Filter(filters3));
        }

        [Fact]
        public void CanPivot()
        {
            var x = GetSUT();
            x.AddRow(new Dictionary<string, object> { { "wwahh", 88 }, { "gwah", "bah!" } }, 80.6);

            var z = x.Pivot("gwah", AggregationAction.Sum);
            var rowValue = z.GetAllRows().Where(r => Convert.ToString(r.MetaData.First()) == "bah!").Single().Value;
            Assert.Equal(80.6 + 79.6, rowValue);

            Assert.Throws<Exception>(() => x.Pivot("ooop", AggregationAction.Sum));

            z = x.Pivot("gwah", AggregationAction.Average);
            rowValue = z.GetAllRows().Where(r => Convert.ToString(r.MetaData.First()) == "bah!").Single().Value;
            Assert.Equal((80.6 + 79.6) / 2.0, rowValue);

            z = x.Pivot("gwah", AggregationAction.Max);
            rowValue = z.GetAllRows().Where(r => Convert.ToString(r.MetaData.First()) == "bah!").Single().Value;
            Assert.Equal(80.6, rowValue);

            z = x.Pivot("gwah", AggregationAction.Min);
            rowValue = z.GetAllRows().Where(r => Convert.ToString(r.MetaData.First()) == "bah!").Single().Value;
            Assert.Equal(79.6, rowValue);
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

            Assert.Throws<Exception>(() => x.Sort(new List<string> { "wwahhzzz" }));
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

            Assert.Throws<Exception>(() => x.BucketTimeAxis("glooop", "bucketz", buckets));

            var z = x.BucketTimeAxis("wwahh", "bucketz", buckets);
            var rowValues = z.GetAllRows();
            Assert.Equal("b1", rowValues[0].ToDictionary(z.DataTypes.Keys.ToArray())["bucketz"]);
            Assert.Equal("b1", rowValues[1].ToDictionary(z.DataTypes.Keys.ToArray())["bucketz"]);
            Assert.Equal("b2", rowValues[2].ToDictionary(z.DataTypes.Keys.ToArray())["bucketz"]);
        }

        [Fact]
        public void CanDifference()
        {
            var x = new ResultCube();
            x.Initialize(new Dictionary<string, Type> { { "gwah", typeof(string) }, { "wwahh", typeof(int) } });
            var y = new ResultCube();
            y.Initialize(new Dictionary<string, Type> { { "gwah", typeof(string) }, { "wwahh", typeof(int) } });

            x.AddRow(new object[] { "woooh", 6 }, 77.6);
            y.AddRow(new object[] { "woooh", 6 }, 77.4);

            var d = x.Difference(y);

            Assert.Equal(0.2, d.GetAllRows().First().Value, 10);

            var z = new ResultCube();
            z.Initialize(new Dictionary<string, Type> { { "gwah", typeof(bool) }, { "wwahh", typeof(int) } });
            Assert.Throws<Exception>(() => x.Difference(z));
            Assert.Throws<Exception>(() => x.QuickDifference(z));

            y.AddRow(new object[] { "woooh", 7 }, 77.4);
            Assert.Throws<Exception>(() => x.QuickDifference(y));
        }

        [Fact]
        public void CanMerge()
        {
            var x = new ResultCube();
            x.Initialize(new Dictionary<string, Type> { { "gwah", typeof(string) }, { "wwahh", typeof(int) } });
            var y = new ResultCube();
            y.Initialize(new Dictionary<string, Type> { { "gwah", typeof(string) }, { "wwahh", typeof(int) } });

            x.AddRow(new object[] { "woooh", 6 }, 77.6);
            y.AddRow(new object[] { "woooh", 6 }, 77.4);

            var d = x.MergeQuick(y);
            Assert.Equal(2, d.GetAllRows().Length);
            d = x.Merge(y);
            Assert.Equal(2, d.GetAllRows().Length);

            var z = new ResultCube();
            z.Initialize(new Dictionary<string, Type> { { "gwah", typeof(bool) }, { "wwahh", typeof(int) } });
            Assert.Throws<Exception>(() => x.MergeQuick(z));
            Assert.Throws<Exception>(() => x.Merge(z));
        }

        [Fact]
        public void ToMatrix()
        {
            var x = GetSUT();
            Assert.Throws<Exception>(() => x.ToMatrix("ooop", "ooop", false));
            Assert.Throws<Exception>(() => x.ToMatrix("wwahh", "ooop", false));

            var m = x.ToMatrix("wwahh", "gwah", false);
            var ms = x.ToMatrix("wwahh", "gwah", true);

            for (var i = 0; i < m.GetLength(0); i++)
                for (var j = 0; j < m.GetLength(1); j++)
                {
                    if (i > 0 && j > 0)
                        if (i != j)
                            Assert.Null(m[i, j]);
                        else
                            Assert.NotNull(m[i, j]);
                }
        }


        [Fact]
        public void IsEqual()
        {
            Assert.True(CubeEx.IsEqual(0.77,0.77));
            Assert.True(CubeEx.IsEqual(77, 77));
            Assert.True(CubeEx.IsEqual('C', 'C'));
            Assert.True(CubeEx.IsEqual(false, false));
            Assert.True(CubeEx.IsEqual(DateTime.Today, DateTime.Today));
            Assert.True(CubeEx.IsEqual(1.6M, 1.6M));

            Assert.False(CubeEx.IsEqual(new List<string>(), 1.6M));
        }
    }
}
