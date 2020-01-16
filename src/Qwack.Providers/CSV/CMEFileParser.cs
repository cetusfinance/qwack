using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;

namespace Qwack.Providers.CSV
{
    public class CMEFileParser
    {
        public static List<CMEFileRecord> Parse(string fileName)
        {
            using var textReader = File.OpenText(fileName);
            using var csv = new CsvReader(textReader);
            csv.Configuration.HasHeaderRecord = true;
            return csv.GetRecords<CMEFileRecord>().ToList();
        }
    }

    public class CMEFileRecord
    {
        public string BizDt { get; set; }
        public string Sym { get; set; }
        public string ID { get; set; }
        public double? StrkPx { get; set; }
        public string SecTyp { get; set; }
        public string MMY { get; set; }
        public string MatDt { get; set; }
        public int? PutCall { get; set; }
        public string Exch { get; set; }
        public string Desc { get; set; }
        public string LastTrdDt { get; set; }
        public double? BidPrice { get; set; }
        public double? OpeningPrice { get; set; }
        public double? SettlePrice { get; set; }
        public double? SettleDelta { get; set; }
        public double? PrevDayVol { get; set; }
        public double? PrevDayOI { get; set; }
        public string UndlyExch { get; set; }
        public string UndlyID { get; set; }
        public string UndlySecTyp { get; set; }
        public string UndlyMMY { get; set; }
    }
}
