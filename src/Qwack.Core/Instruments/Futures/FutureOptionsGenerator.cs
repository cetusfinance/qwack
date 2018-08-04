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
        private decimal StrikeRounding;
        public OptionDateGeneratorKind Kind { get; set; }
        public OptionStrikeRuleKind StrikeRule { get; set; }
        public string OffsetSize { get; set; }
        public string Generator { get; set; }
        private int numberOfPeriodsToOffset;
        private string periodToOffset;
        private List<KeyValuePair<int, double>> Strikes;
        private List<CBOTOptionRule> CBOTRules;

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

            var upperPrice = (decimal)atmPrice;
            var lowerPrice = (decimal)atmPrice;
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

            var X = CBOTRules.OrderBy(x => x.appliesWhenNMonthsOut).ToList();
            foreach (var R in X)
            {
                if (R.appliesWhenNMonthsOut == 0 || nMonthsOut <= R.appliesWhenNMonthsOut)
                {
                    var upperPrice = atmPrice;
                    var lowerPrice = atmPrice;
                    var strikeModAbove = (decimal)R.strikeIncrementAboveBoundary;
                    var strikeModBelow = (decimal)R.strikeIncrementBelowBoundary;

                    while ((upperPrice < (1 + (decimal)R.percentRange) * atmPrice) || (lowerPrice > (1 - (decimal)R.percentRange) * atmPrice))
                    {
                        upperPrice = upperPrice + (upperPrice >= (decimal)R.boundary ? strikeModAbove : strikeModBelow);
                        lowerPrice = lowerPrice - (lowerPrice <= (decimal)R.boundary ? strikeModBelow : strikeModAbove);
                        if (!returnValues.Contains(upperPrice) && (upperPrice <= (1 + (decimal)R.percentRange) * atmPrice))
                            returnValues.Add(upperPrice);
                        if (!returnValues.Contains(lowerPrice) && (lowerPrice >= (1 - (decimal)R.percentRange) * atmPrice))
                            returnValues.Add(lowerPrice);
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
            public int appliesWhenNMonthsOut { get; set; }
            public double boundary { get; set; }
            public double strikeIncrementAboveBoundary { get; set; }
            public double strikeIncrementBelowBoundary { get; set; }
            public double percentRange { get; set; }
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
                        var C = new CBOTOptionRule();
                        C.appliesWhenNMonthsOut = int.Parse(E.Element("appliesWhenNMonthsOut").Value);
                        C.boundary = double.Parse(E.Element("boundary").Value, CultureInfo.InvariantCulture.NumberFormat);
                        C.strikeIncrementAboveBoundary = double.Parse(E.Element("strikeIncrementAboveBoundary").Value, CultureInfo.InvariantCulture.NumberFormat);
                        C.strikeIncrementBelowBoundary = double.Parse(E.Element("strikeIncrementBelowBoundary").Value, CultureInfo.InvariantCulture.NumberFormat);
                        C.percentRange = double.Parse(E.Element("percentRange").Value, CultureInfo.InvariantCulture.NumberFormat);

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
                    numberOfPeriodsToOffset = int.Parse(OffsetSize.Substring(0, OffsetSize.Length - 1));
                    periodToOffset = OffsetSize[OffsetSize.Length - 1].ToString();
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
