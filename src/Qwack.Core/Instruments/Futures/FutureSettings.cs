using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Qwack.Dates;

namespace Qwack.Core.Instruments.Futures
{
    public class FutureSettings
    {
        private ICalendarProvider _calendarProvider;

        public List<string> Names { get; set; }
        public FutureDatesGenerator ExpiryGen { get; set; }
        public FutureDatesGenerator RollGen { get; set; }
        public List<string> Months { get; set; }
        public TimeZoneInfo TimeZone { get; set; }
        public List<TimeSpan> MarketCloseTime { get; set; }

        public List<DateTime> MarketOpenRulesValidUntil { get; set; }
        public List<TimeSpan> MarketOpenTime { get; set; }
        public List<int> MarketOpenModifier { get; set; }
        public List<MarketOpenOverride> MarketOpenTimeOverride { get; set; }

        public List<TimeSpan> MarketCloseVWAPLength { get; set; }
        public List<DateTime> MarketCloseValidTo { get; set; }

        public List<MarketShutRuleSet> MarketShutRules { get; set; }
        public List<DateTime> MarketShutRulesValidUntil { get; set; }

        public FutureOptionsGenerator Options { get; set; }

        public FutureSettings(ICalendarProvider calendarProvider) => _calendarProvider = calendarProvider;

        public void LoadXml(XElement element)
        {
            Names = element.Elements("Name").Select(e => e.Value).ToList();
            ExpiryGen = new FutureDatesGenerator();
            ExpiryGen.LoadXml(element.Element("Expiry"));
            RollGen = new FutureDatesGenerator();
            RollGen.LoadXml(element.Element("RollDate"));
            Months = new List<string>(element.Element("FutureMonths").Value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
            TimeZone = TimeZoneInfo.FindSystemTimeZoneById(element.Element("TimeZone").Value);

            MarketCloseTime = new List<TimeSpan>();
            MarketCloseValidTo = new List<DateTime>();
            MarketCloseVWAPLength = new List<TimeSpan>();

            MarketOpenTime = new List<TimeSpan>();
            MarketOpenTimeOverride = new List<MarketOpenOverride>();
            MarketOpenRulesValidUntil = new List<DateTime>();
            MarketOpenModifier = new List<int>();

            foreach (var closeInfo in element.Elements("MarketClose"))
            {
                MarketCloseTime.Add(TimeSpan.Parse(closeInfo.Element("Time").Value));
                MarketCloseVWAPLength.Add(TimeSpan.Parse(closeInfo.Element("VWAPLength").Value));

                if (closeInfo.Element("ValidUntil") == null)
                {

                    MarketCloseValidTo.Add(DateTime.MaxValue);
                }
                else
                {
                    MarketCloseValidTo.Add(DateTime.Parse(closeInfo.Element("ValidUntil").Value));
                }
            }

            MarketShutRules = new List<MarketShutRuleSet>();
            MarketShutRulesValidUntil = new List<DateTime>();

            foreach (var marketRule in element.Elements("MarketShutRule"))
            {
                if (marketRule.Element("ValidUntil") == null)
                {
                    MarketShutRulesValidUntil.Add(DateTime.MaxValue);
                }
                else
                {
                    MarketShutRulesValidUntil.Add(DateTime.Parse(marketRule.Element("ValidUntil").Value));
                }
                var mcRule = new MarketShutRuleSet(_calendarProvider);
                mcRule.LoadFromXml(marketRule, RollGen.Calendar, TimeZone);
                MarketShutRules.Add(mcRule);
            }

            foreach (var marketRule in element.Elements("MarketOpen"))
            {
                if (marketRule.Element("ValidUntil") == null)
                {
                    MarketOpenRulesValidUntil.Add(DateTime.MaxValue);
                }
                else
                {
                    MarketOpenRulesValidUntil.Add(DateTime.Parse(marketRule.Element("ValidUntil").Value));
                }

                var DefaultOpen = TimeSpan.Parse(marketRule.Element("Time").Value);
                var DefaultDayModifier = int.Parse(marketRule.Element("DayModifier").Value);

                MarketOpenTime.Add(DefaultOpen);
                MarketOpenModifier.Add(DefaultDayModifier);

                if (element.Elements("Override") != null)
                {
                    foreach (var overRideElement in element.Elements("Override"))
                    {
                        var DoW = (DayOfWeek)Enum.Parse(typeof(DayOfWeek), overRideElement.Element("DayOfWeek").Value);
                        var OpenTime = TimeSpan.Parse(overRideElement.Element("Time").Value);
                        var DayModifier = int.Parse(overRideElement.Element("DayModifier").Value);

                        var moRule = new MarketOpenOverride(DoW, OpenTime, DayModifier);
                        MarketOpenTimeOverride.Add(moRule);
                    }
                }
            }

            if (element.Element("LotSize") != null)
                LotSize = double.Parse(element.Element("LotSize").Value, CultureInfo.InvariantCulture.NumberFormat);

            if (element.Element("PriceMultiplier") != null)
                PriceMultiplier = double.Parse(element.Element("PriceMultiplier").Value, CultureInfo.InvariantCulture.NumberFormat);

            if (element.Element("TickSize") != null)
                TickSize = double.Parse(element.Element("TickSize").Value, CultureInfo.InvariantCulture.NumberFormat);

            if (element.Element("OptionExpiry") != null)
            {
                //Setup Option Expiry calculator
                Options = new FutureOptionsGenerator();
                Options.LoadXml(element.Element("OptionExpiry"), this);
            }
        }

        public double LotSize { get; set; }
        public double PriceMultiplier { get; set; }
        public double TickSize { get; set; }
    }
}
