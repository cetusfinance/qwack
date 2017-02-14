using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Extensions.PlatformAbstractions;
using Qwack.Random.MersenneTwister;
using Xunit;

namespace Qwack.Math.Tests.Random
{
    public class MTFacts
    {
        private static readonly string _IntFile = System.IO.Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, "mt19937-64-Ints.txt");

        [Fact]
        public void TestMT19937IntegerOutput()
        {
            var testNumbers = new List<ulong>();
            foreach (var l in File.ReadLines(_IntFile).Skip(1))
            {
                var values = l.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < values.Length; i++)
                {
                    testNumbers.Add(ulong.Parse(values[i]));
                }
            }
            ulong[] init = { 0x12345UL, 0x23456UL, 0x34567UL, 0x45678UL };
            var mt = new MersenneTwister64(init, 4);

            for (int i = 0; i < testNumbers.Count; i++)
            {
                Assert.Equal(testNumbers[i], mt.GenerateInteger());
            }
        }
    }
}
