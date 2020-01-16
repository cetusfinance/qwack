using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Qwack.Futures
{
    public class FutureOptionsGenerator
    {
        //private readonly FutureSettings _parent;
        public decimal StrikeRounding { get; set; }
        public OptionDateGeneratorKind Kind { get; set; }
        public OptionStrikeRuleKind StrikeRule { get; set; }
        public string OffsetSize { get; set; }
        public string Generator { get; set; }
        public int NumberOfPeriodsToOffset { get; set; }
        public string PeriodToOffset { get; set; }
        public List<KeyValuePair<int, double>> Strikes { get; set; }
        public List<CBOTOptionRule> CBOTRules { get; set; }

        public decimal RoundToStrike(decimal price)
        {
            var modValue = price % StrikeRounding;

            if (modValue >= StrikeRounding / 2.0M)
            {
                price = price - modValue + StrikeRounding;
            }
            else
            {
                price -= modValue;
            }
            return System.Math.Round(price, 8);
        }

        //public List<decimal> GetStrikesAroundPrice(decimal price, FutureCode contract, DateTime valueDate)
        //{
        //    if (StrikeRule == OptionStrikeRuleKind.AboveBelowAbsolute)
        //        return GetStrikesAroundPrice_AboveBelow(price);
        //    else if (StrikeRule == OptionStrikeRuleKind.CBOT)
        //    {
        //        var nMonthsOut = 1;
        //        string MC = contract.OriginalCode;
        //        if (FuturesFunctions.GetFutureExpiry(MC) >= valueDate)
        //        {
        //            string MC0 = FuturesFunctions.GetFrontMonthCode(contract.Prefix, valueDate, false, true);
        //            while (MC0 != MC)
        //            {
        //                MC = FuturesFunctions.GetPreviousCode(MC);
        //                nMonthsOut++;
        //            }
        //        }
        //        return GetStrikesAroundPrice_CBOT(price, nMonthsOut);
        //    }
        //    else
        //        throw new NotImplementedException();
        //}

        private List<decimal> GetStrikesAroundPrice_AboveBelow(decimal price)
        {
            var returnValues = new List<decimal>();
            var atmPrice = RoundToStrike(price);
            returnValues.Add(atmPrice);

            var upperPrice = atmPrice;
            var lowerPrice = atmPrice;
            foreach (var kv in Strikes.OrderBy(x => x.Value))
            {
                for (var i = 0; i < kv.Key; i++)
                {
                    upperPrice += (decimal)kv.Value;
                    lowerPrice -= (decimal)kv.Value;
                    returnValues.Add(upperPrice);
                    returnValues.Add(lowerPrice);
                }
            }
            return returnValues;
        }
        private List<decimal> GetStrikesAroundPrice_CBOT(decimal price, int nMonthsOut)
        {
            var returnValues = new List<decimal>();
            var atmPrice = RoundToStrike(price);
            returnValues.Add(atmPrice);

            var X = CBOTRules.OrderBy(x => x.AppliesWhenNMonthsOut).ToList();
            foreach (var R in X)
            {
                if (R.AppliesWhenNMonthsOut == 0 || nMonthsOut <= R.AppliesWhenNMonthsOut)
                {
                    var upperPrice = atmPrice;
                    var lowerPrice = atmPrice;
                    var strikeModAbove = (decimal)R.StrikeIncrementAboveBoundary;
                    var strikeModBelow = (decimal)R.StrikeIncrementBelowBoundary;

                    while ((upperPrice < (1 + (decimal)R.PercentRange) * atmPrice) || (lowerPrice > (1 - (decimal)R.PercentRange) * atmPrice))
                    {
                        upperPrice += (upperPrice >= (decimal)R.Boundary ? strikeModAbove : strikeModBelow);
                        lowerPrice -= (lowerPrice <= (decimal)R.Boundary ? strikeModBelow : strikeModAbove);
                        if (!returnValues.Contains(upperPrice) && (upperPrice <= (1 + (decimal)R.PercentRange) * atmPrice))
                        {
                            returnValues.Add(upperPrice);
                        }
                        if (!returnValues.Contains(lowerPrice) && (lowerPrice >= (1 - (decimal)R.PercentRange) * atmPrice))
                        {
                            returnValues.Add(lowerPrice);
                        }
                    }
                }
            }

            returnValues.Sort();
            return returnValues;
        }

        //public DateTime GetOptionExpiry(string futureCode)
        //{
        //    if (Kind == OptionDateGeneratorKind.Offset)
        //    {
        //        if (numberOfPeriodsToOffset > 0)
        //        {
        //            return DateFunctions.AddPeriod(FuturesFunctions.GetFutureExpiry(futureCode), "F", _Parent.ExpiryGen.Calendar, periodToOffset, numberOfPeriodsToOffset);
        //        }
        //        else
        //        {
        //            return DateFunctions.SubtractPeriod(FuturesFunctions.GetFutureExpiry(futureCode), "P", _Parent.ExpiryGen.Calendar, periodToOffset, Math.Abs(numberOfPeriodsToOffset));
        //        }

        //    }
        //    else if (Kind == OptionDateGeneratorKind.GenCode)
        //    {
        //        DateTime FutExp = FuturesFunctions.GetFutureExpiry(futureCode);
        //        return DateFunctions.SpecificWeekDayInMonthCode(FutExp, Generator, _Parent.ExpiryGen.Calendar);
        //    }
        //    else
        //    {
        //        throw new NotImplementedException();
        //    }
        //}

        public enum OptionDateGeneratorKind
        {
            Offset,
            GenCode,
            OwnDateSettings
        }
        public enum OptionStrikeRuleKind
        {
            AboveBelowAbsolute,
            CBOT,
            CBOTwithBoundary
        }

        public class CBOTOptionRule
        {
            public int AppliesWhenNMonthsOut { get; set; }
            public double Boundary { get; set; }
            public double StrikeIncrementAboveBoundary { get; set; }
            public double StrikeIncrementBelowBoundary { get; set; }
            public double PercentRange { get; set; }
        }
    }
}
