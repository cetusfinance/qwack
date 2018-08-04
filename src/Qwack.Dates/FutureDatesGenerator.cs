using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace Qwack.Dates
{
    public class FutureDatesGenerator
    {
        private bool _DoMToStartIsNumber;

        public string Calendar { get; set; }
        public int MonthModifier { get; set; }
        public object DayOfMonthToStart { get; set; }
        public string DateOffsetModifier { get; set; }
        public bool DoMToStartIsNumber { get; set; }
        public bool NeverExpires { get; set; }
        public string FixedFuture { get; set; }

        public void LoadXml(XElement element)
        {
            Calendar = element.Element("Calendar").Value;
            if (element.Element("NEVEREXPIRES") != null)
            {
                NeverExpires = true;
                FixedFuture = element.Element("FixedFuture").Value;
            }
            else
            {
                MonthModifier = (int)element.Element("MonthModifier");
                if (int.TryParse(element.Element("DayOfMonthToStart").Value.ToString(), out var X))
                {
                    DayOfMonthToStart = X;
                    _DoMToStartIsNumber = true;
                }
                else
                {
                    DayOfMonthToStart = element.Element("DayOfMonthToStart").Value;
                    _DoMToStartIsNumber = false;
                }

                DateOffsetModifier = (string)element.Element("DayOfMonthModifier");
            }
        }
    }
}
