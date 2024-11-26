using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Qwack.Dates;
using Qwack.Excel.Instruments;
using Qwack.Providers.Json;
using static ExcelDna.Integration.ExcelMissing;

namespace Qwack.Excel.Tests.Instruments
{
    public class FundingInstrumentFunctionsFacts
    {
        private readonly DateTime _today = new DateTime(2020, 01, 01);
        [Fact]
        public void CreateFRA_Facts()
        {
            Assert.Equal("Rate index yarrr not found in cache", FundingInstrumentFunctions.CreateFRA("gleugh", _today, "3x6", "yarrr", "USD", 0.05, 1e6, "F.USD", "D.USD", Value, Value, Value, Value));
            var z = FundingInstrumentFunctions.CreateRateIndex("yarrr", "USD", "3m", "Act365F", "Act365F", "3m", "NYC", "2b", "F");
            Assert.Equal("Could not parse pay/rec - waaah", FundingInstrumentFunctions.CreateFRA("gleugh", _today, "3x6", "yarrr", "USD", 0.05, 1e6, "F.USD", "D.USD", Value, "waaah", Value, Value));
            Assert.Equal("Could not parse FRA discounting type - waaah", FundingInstrumentFunctions.CreateFRA("gleugh", _today, "3x6", "yarrr", "USD", 0.05, 1e6, "F.USD", "D.USD", "waaah", "Pay", Value, Value));
            Assert.Equal("gleugh¬0", FundingInstrumentFunctions.CreateFRA("gleugh", _today, "3x6", "yarrr", "USD", 0.05, 1e6, "F.USD", "D.USD", "Isda", "Pay", Value, Value));
        }

        [Fact]
        public void CreateFxForward_Facts() => Assert.Equal("meh¬0", FundingInstrumentFunctions.CreateFxForward("meh", DateTime.Today, "USD", "ZAR", 123, 17, "Z.1", "Z.1", Value));

        [Fact]
        public void CreateIRS_Facts()
        {
            Assert.Equal("Rate index yarrrh not found in cache", FundingInstrumentFunctions.CreateIRS("gleughh", _today, "3y", "yarrrh", 0.05, 1e6, "F.USD", "D.USD", Value, Value, Value));
            var z = FundingInstrumentFunctions.CreateRateIndex("yarrrh", "USD", "3m", "Act365F", "Act365F", "3m", "NYC", "2b", "F");
            Assert.Equal("Could not parse pay/rec - waaah", FundingInstrumentFunctions.CreateIRS("gleughh", _today, "3y", "yarrrh", 0.05, 1e6, "F.USD", "D.USD", "waaah", Value, Value));
            Assert.Equal("gleughh¬0", FundingInstrumentFunctions.CreateIRS("gleughh", _today, "3y", "yarrrh", 0.05, 1e6, "F.USD", "D.USD", Value, Value, Value));
        }

        [Fact]
        public void CreateSTIRFromCode_Facts()
        {
            Assert.Equal("Rate index yarrgh not found in cache", FundingInstrumentFunctions.CreateSTIRFromCode("jarr", _today, "EDZ8", "yarrgh", 97.75, 1000, 0, "F.USD", Value, Value));
            var z = FundingInstrumentFunctions.CreateRateIndex("yarrgh", "USD", "3m", "Act365F", "Act365F", "3m", "NYC", "2b", "F");
            Assert.Equal("jarr¬0", FundingInstrumentFunctions.CreateSTIRFromCode("jarr", _today, "EDZ8", "yarrgh", 97.75, 1000, 0, "F.USD", Value, Value));
        }

        [Fact]
        public void CreateOISFutureFromCode_Facts()
        {
            Assert.Equal("Rate index wooo not found in cache", FundingInstrumentFunctions.CreateOISFutureFromCode("jarrf", _today, "EDZ8", "wooo", 97.75, 1000, "F.USD", Value, Value));
            var z = FundingInstrumentFunctions.CreateRateIndex("wooo", "USD", "3m", "Act365F", "Act365F", "3m", "NYC", "2b", "F");
            Assert.Equal("jarrf¬0", FundingInstrumentFunctions.CreateOISFutureFromCode("jarrf", _today, "EDZ8", "wooo", 97.75, 1000, "F.USD", Value, Value));
        }

        [Fact]
        public void CreateIRBasisSwap_Facts()
        {
            Assert.Equal("Rate index harrP not found in cache", FundingInstrumentFunctions.CreateIRBasisSwap("glooop", _today, "8y", "harrP", "harrR", 0.05, Value, 1e6, "F.PAY", "F.REC", "DISCO", Value, Value));
            var z = FundingInstrumentFunctions.CreateRateIndex("harrP", "USD", "3m", "Act365F", "Act365F", "3m", "NYC", "2b", "F");
            Assert.Equal("Rate index harrR not found in cache", FundingInstrumentFunctions.CreateIRBasisSwap("glooop", _today, "8y", "harrP", "harrR", 0.05, Value, 1e6, "F.PAY", "F.REC", "DISCO", Value, Value));
            z = FundingInstrumentFunctions.CreateRateIndex("harrR", "USD", "3m", "Act365F", "Act365F", "3m", "NYC", "2b", "F");
            Assert.Equal("glooop¬0", FundingInstrumentFunctions.CreateIRBasisSwap("glooop", _today, "8y", "harrP", "harrR", 0.05, Value, 1e6, "F.PAY", "F.REC", "DISCO", Value, Value));
        }

        [Fact]
        public void CreateFixedRateLoanDepo_Facts()
        {
            Assert.Equal("Could not parse daycount basis - mep", FundingInstrumentFunctions.CreateFixedRateLoanDepo("zlagh", _today, _today, 0.05, "mep", "USD", 123, "DISCO"));
            Assert.Equal("zlagh¬0", FundingInstrumentFunctions.CreateFixedRateLoanDepo("zlagh", _today, _today, 0.05, "Act360", "USD", 123, "DISCO"));
        }

        [Fact]
        public void CreateFundingInstrumentCollection_Facts()
        {
            var z = FundingInstrumentFunctions.CreateFxForward("mehg", _today, "USD", "ZAR", 123, 17, "Z.1", "Z.1", Value);
            var e = new object[0, 0];
            Assert.Equal("zzlagh¬0", FundingInstrumentFunctions.CreateFundingInstrumentCollection("zzlagh", new object[,] { { z } }, e, e, e, e, e, e, e, e));
        }

        [Fact]
        public void CreateRateIndex_Facts()
        {
            Assert.Equal("Could not parse fixed daycount - ffff", FundingInstrumentFunctions.CreateRateIndex("haarrrr", "USD", "3m", "flfl", "ffff", "3m", "NYC", "2b", "F"));
            Assert.Equal("Could not parse float daycount - flfl", FundingInstrumentFunctions.CreateRateIndex("haarrrr", "USD", "3m", "flfl", "Act365F", "3m", "NYC", "2b", "F"));
            Assert.Equal("Could not parse roll convention - FmFm", FundingInstrumentFunctions.CreateRateIndex("haarrrr", "USD", "3m", "Act365F", "Act365F", "3m", "NYC", "2b", "FmFm"));
            Assert.Equal("Calendar Jozi not found in cache", FundingInstrumentFunctions.CreateRateIndex("haarrrr", "USD", "3m", "Act365F", "Act365F", "3m", "Jozi", "2b", "F"));
            Assert.Equal("haarrrr¬0", FundingInstrumentFunctions.CreateRateIndex("haarrrr", "USD", "3m", "Act365F", "Act365F", "3m", "NYC", "2b", "F"));
        }

        [Fact]
        public void CreateFxPair_Facts()
        {
            Assert.Equal("Calendar jozi not found in cache", FundingInstrumentFunctions.CreateFxPair("aiyaiy", "USD", "ZAR", "jozi", "2b"));
            Assert.Equal("aiyaiy¬0", FundingInstrumentFunctions.CreateFxPair("aiyaiy", "USD", "ZAR", "NYC", "2b"));
        }
    }
}
