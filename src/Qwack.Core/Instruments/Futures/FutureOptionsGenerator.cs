using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Qwack.Core.Instruments.Futures
{
    public class FutureOptionsGenerator
    {
        private FutureSettings _parent;
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
                price = price - modValue;
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
                        upperPrice = upperPrice + (upperPrice >= (decimal)R.Boundary ? strikeModAbove : strikeModBelow);
                        lowerPrice = lowerPrice - (lowerPrice <= (decimal)R.Boundary ? strikeModBelow : strikeModAbove);
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

        public void LoadXml(XElement element, FutureSettings parent)
        {
            _parent = parent;

            Kind = (OptionDateGeneratorKind)Enum.Parse(typeof(OptionDateGeneratorKind), element.Element("Type").Value);

            StrikeRounding = decimal.Parse(element.Element("StrikeRounding").Value, CultureInfo.InvariantCulture.NumberFormat);

            if (element.Element("StrikeRule") == null)
            {
                StrikeRule = OptionStrikeRuleKind.AboveBelowAbsolute;
                Strikes = element.Element("Strikes").Elements("Strike").Select(x => new KeyValuePair<int, double>(int.Parse(x.Attribute("AboveAndBelow").Value), double.Parse(x.Value, CultureInfo.InvariantCulture.NumberFormat))).ToList();
            }
            else
            {
                StrikeRule = (OptionStrikeRuleKind)Enum.Parse(typeof(OptionStrikeRuleKind), element.Element("StrikeRule").Value);
                if (StrikeRule == OptionStrikeRuleKind.CBOT)
                {
                    CBOTRules = new List<CBOTOptionRule>();
                    foreach (var E in element.Elements("Rule"))
                    {
                        var C = new CBOTOptionRule
                        {
                            AppliesWhenNMonthsOut = int.Parse(E.Element("appliesWhenNMonthsOut").Value),
                            Boundary = double.Parse(E.Element("boundary").Value, CultureInfo.InvariantCulture.NumberFormat),
                            StrikeIncrementAboveBoundary = double.Parse(E.Element("strikeIncrementAboveBoundary").Value, CultureInfo.InvariantCulture.NumberFormat),
                            StrikeIncrementBelowBoundary = double.Parse(E.Element("strikeIncrementBelowBoundary").Value, CultureInfo.InvariantCulture.NumberFormat),
                            PercentRange = double.Parse(E.Element("percentRange").Value, CultureInfo.InvariantCulture.NumberFormat)
                        };

                        CBOTRules.Add(C);
                    }
                }
                else if (StrikeRule == OptionStrikeRuleKind.AboveBelowAbsolute)
                {
                    Strikes = element.Element("Strikes").Elements("Strike").Select(x => new KeyValuePair<int, double>(int.Parse(x.Attribute("AboveAndBelow").Value), double.Parse(x.Value, CultureInfo.InvariantCulture.NumberFormat))).ToList();
                }
                else
                    throw new NotImplementedException();
            }

            switch (Kind)
            {
                case OptionDateGeneratorKind.Offset:
                    OffsetSize = element.Element("OffsetSize").Value;
                    NumberOfPeriodsToOffset = int.Parse(OffsetSize.Substring(0, OffsetSize.Length - 1));
                    PeriodToOffset = OffsetSize[OffsetSize.Length - 1].ToString();
                    break;
                case OptionDateGeneratorKind.GenCode:
                    Generator = element.Element("Generator").Value;
                    break;
                case OptionDateGeneratorKind.OwnDateSettings:
                    throw new NotImplementedException();
            }
        }
    }
}
