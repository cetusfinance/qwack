using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Qwack.Excel.Utils;
using Xunit;
using static ExcelDna.Integration.ExcelMissing;

namespace Qwack.Excel.Tests.Utils
{
    public class ExcelUtilsFacts
    {
        [Fact]
        public void SessionItemFact()
        {
            var z = new SessionItem<string>() { Name = "woo", Value = string.Empty };
            Assert.Equal("woo|0", z.ToString());
        }

        [Fact]
        public void NowFact()
        {
            var x = (string)ExcelUtils.QUtils_Now();
            var xt = DateTime.Parse(x);
            Assert.True(xt <= DateTime.Now);
        }

        [Fact]
        public void UniqueFact()
        {
            var data = new object[,] { { 2, 2, 3, 1 } };
            var result = ((object[]) ExcelUtils.QUtils_Unique(data, "Descending")).Select(x=>(int)x).ToArray();
            Assert.True(Enumerable.SequenceEqual(new[] { 3, 2, 1 }, result));
            result = ((object[])ExcelUtils.QUtils_Unique(data, Value)).Select(x => (int)x).ToArray();
            Assert.True(Enumerable.SequenceEqual(new[] { 2, 3, 1 }, result));
            result = ((object[])ExcelUtils.QUtils_Unique(data, "Ascending")).Select(x => (int)x).ToArray();
            Assert.True(Enumerable.SequenceEqual(new[] { 1, 2, 3 }, result));
        }

        [Fact]
        public void SortFact()
        {
            var data = new object[,] { { 2, 2, 3, 1 } };
            var result = ((object[])ExcelUtils.QUtils_Sort(data, "Descending")).Select(x => (int)x).ToArray();
            Assert.True(Enumerable.SequenceEqual(new[] { 3, 2, 2, 1 }, result));
            result = ((object[])ExcelUtils.QUtils_Sort(data, "Ascending")).Select(x => (int)x).ToArray();
            Assert.True(Enumerable.SequenceEqual(new[] { 1, 2, 2, 3 }, result));
        }

        [Fact]
        public void RemoveWhitespaceFact()
        {
            Assert.Equal("boom", (string)ExcelUtils.QUtils_RemoveWhitespace("  boo m   ", Value));
            Assert.Equal("boom", (string)ExcelUtils.QUtils_RemoveWhitespace("  boo--m   ", " -"));
        }

        [Fact]
        public void FilterFact()
        {
            var data = new object[] { 2, ExcelDna.Integration.ExcelEmpty.Value, 3, "" };
            var data2 = new object[] { 1, 2, 3, 4 };
            var result = ((object[])ExcelUtils.QUtils_Filter(data2, data)).Select(x => (int)x).ToArray();
            Assert.True(Enumerable.SequenceEqual(new[] { 1, 3 }, result));
        }

        [Fact]
        public void TrimFacts()
        {
            Assert.Equal("boo m", (string)ExcelUtils.QUtils_Trim("  boo m   ", Value));
            Assert.Equal("boo--m", (string)ExcelUtils.QUtils_Trim("  boo--m-   ", " -"));
        }

        [Fact]
        public void GoodValuesFact()
        {
            var data = new object[] { 2, ExcelDna.Integration.ExcelEmpty.Value, 3, ExcelDna.Integration.ExcelError.ExcelErrorNA };
            var result = ((object[])ExcelUtils.QUtils_GoodValues(data)).Select(x => (int)x).ToArray();
            Assert.True(Enumerable.SequenceEqual(new[] { 2, 3 }, result));
        }

        [Fact]
        public void ToNumbersFact()
        {
            var data = new object[] { "2.0", " 3 300.6", "4,200" };
            var result = ((object[])ExcelUtils.QUtils_ToNumbers(data)).Select(x => (double)x).ToArray();
            Assert.True(Enumerable.SequenceEqual(new[] { 2.0, 3300.6, 4200.0 }, result));
        }

        [Fact]
        public void RangeToXSVFact()
        {
            var data = new object[] { "A", "B", "C" };
            var result = (string)ExcelUtils.QUtils_RangeToXSV(data,Value);
            Assert.Equal("A,B,C",result);
        }
    }
}
