using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Dates;

namespace Qwack.Core.Instruments.Asset
{
    public class Forward : IInstrument
    {
        public string TradeId { get; set; }

        public double Notional { get; set; }
        public TradeDirection Direction { get; set; }

        public DateTime ExpiryDate { get; set; }

        public Calendar FixingCalendar { get; set; }
        public Calendar PaymentCalendar { get; set; }
        public Frequency SpotLag { get; set; }
        public Frequency PaymentLag { get; set; }
        public double Strike { get; set; }
        public string AssetId { get; set; }
        public Currency PaymentCurrency { get; set; }
        public string FxFixingSource { get; set; }
        public string DiscountCurve { get; set; }
    }
}
