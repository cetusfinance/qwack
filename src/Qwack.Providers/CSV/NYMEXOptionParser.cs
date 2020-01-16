using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;

namespace Qwack.Providers.CSV
{
    public class NYMEXOptionParser
    {
        public static List<NYMEXOptionRecord> Parse(string fileName)
        {
            using var textReader = File.OpenText(fileName);
            using var csv = new CsvReader(textReader);
            csv.Configuration.HasHeaderRecord = true;
            csv.Configuration.RegisterClassMap<NYMEXOptionRecordMap>();

            return csv.GetRecords<NYMEXOptionRecord>().ToList();
        }
    }

    public class NYMEXOptionRecord
    {
        public string Symbol { get; set; }
        public int ContractMonth { get; set; }
        public int ContractYear { get; set; }
        public int? ContractDay { get; set; }
        public string PutCall { get; set; }
        public double Strike { get; set; }
        public string Contract { get; set; }
        public string Description { get; set; }
        public double? Open { get; set; }
        public double? High { get; set; }
        public double? Low { get; set; }
        public double? Last { get; set; }
        public double Settle { get; set; }
        public string Change { get; set; }
        public int Volume { get; set; }
        public double PriorSettle { get; set; }
        public int? PriorVolume { get; set; }
        public int? PriorOI { get; set; }
        public string TradeDate { get; set; }
    }

    public sealed class NYMEXOptionRecordMap : ClassMap<NYMEXOptionRecord>
    {
        public NYMEXOptionRecordMap()
        {
            Map(m => m.Symbol).Name("PRODUCT SYMBOL");
            Map(m => m.ContractMonth).Name("CONTRACT MONTH");
            Map(m => m.ContractYear).Name("CONTRACT YEAR");
            Map(m => m.ContractDay).Name("CONTRACT DAY");
            Map(m => m.PutCall).Name("PUT/CALL");
            Map(m => m.Strike).Name("STRIKE").Default(-1.0);
            Map(m => m.Contract).Name("CONTRACT");
            Map(m => m.Description).Name("PRODUCT DESCRIPTION");
            Map(m => m.Open).Name("OPEN");
            Map(m => m.High).Name("HIGH");
            Map(m => m.Low).Name("LOW");
            Map(m => m.Last).Name("LAST");
            Map(m => m.Settle).Name("SETTLE").Default(-1.0);
            Map(m => m.Change).Name("PT CHG");
            Map(m => m.Volume).Name("EST. VOL");
            Map(m => m.PriorSettle).Name("PRIOR SETTLE").Default(-1.0); 
            Map(m => m.PriorVolume).Name("PRIOR VOL");
            Map(m => m.PriorOI).Name("PRIOR INT");
            Map(m => m.TradeDate).Name("TRADEDATE");
        }

    }
}
