using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Dates;

namespace Qwack.Core.Instruments.Asset
{
    public class AsianSwap
    {
        public double Notional { get; set; }
        public TradeDirection Direction { get; set; }

        public DateTime AverageStartDate { get; set; }
        public DateTime AverageEndDate { get; set; }
        public DateTime[] FixingDates { get; set; }
        public Calendar FixingCalendar { get; set; }
        public Calendar PaymentCalendar { get; set; }
        public Frequency SpotLag { get; set; }
        public Frequency PaymentLag { get; set; }
        public DateTime PaymentDate { get; set; }
        public double Strike { get; set; }
        public string AssetId { get; set; }
        public Currency PaymentCurrency { get; set; }
        public string FxFixingSource { get; set; }
        public FxConversionType FxConversionType { get; set; }
    }
}
