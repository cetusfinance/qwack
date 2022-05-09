using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;

namespace Qwack.Providers.CSV
{
    public class NYMEXFutureParser
    {
        private readonly ConcurrentDictionary<string, List<NYMEXFutureRecord>> _cache = new();
        private static readonly NYMEXFutureParser instance = new();

        public List<NYMEXFutureRecord> Parse(string fileName)
        {
            if (_cache.TryGetValue(fileName, out var record))
                return record;

            var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var sr = new StreamReader(fs);
            var cfg = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                BadDataFound = null,
            };

            using var csv = new CsvReader(sr, cfg);
            csv.Context.RegisterClassMap<NYMEXFutureRecordMap>();
            var list = csv.GetRecords<NYMEXFutureRecord>().ToList();
            _cache.TryAdd(fileName, list);

            return list;
        }

        public List<NYMEXFutureRecord> Parse(StreamReader sr)
        {
            var cfg = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                BadDataFound = null,
            };

            using var csv = new CsvReader(sr, cfg);
            csv.Context.RegisterClassMap<NYMEXFutureRecordMap>();
            var list = csv.GetRecords<NYMEXFutureRecord>().ToList();

            return list;
        }

        static NYMEXFutureParser()
        {
        }

        private NYMEXFutureParser()
        {
        }

        public static NYMEXFutureParser Instance => instance;
    }

    public class NYMEXFutureRecord
    {
        public string Symbol { get; set; }
        public int ContractMonth { get; set; }
        public int ContractYear { get; set; }
        public int? ContractDay { get; set; }
        public string Contract { get; set; }
        public string Description { get; set; }
        public double? Open { get; set; }
        public double? High { get; set; }
        public double? Low { get; set; }
        public double? Last { get; set; }
        public double? Settle { get; set; }
        public string Change { get; set; }
        public int Volume { get; set; }
        public double? PriorSettle { get; set; }
        public int? PriorVolume { get; set; }
        public int? PriorOI { get; set; }
        public string TradeDate { get; set; }
    }

    public sealed class NYMEXFutureRecordMap : ClassMap<NYMEXFutureRecord>
    {
        public NYMEXFutureRecordMap()
        {
            Map(m => m.Symbol).Name("PRODUCT SYMBOL");
            Map(m => m.ContractMonth).Name("CONTRACT MONTH");
            Map(m => m.ContractYear).Name("CONTRACT YEAR");
            Map(m => m.ContractDay).Name("CONTRACT DAY");
            Map(m => m.Contract).Name("CONTRACT");
            Map(m => m.Description).Name("PRODUCT DESCRIPTION");
            Map(m => m.Open).Name("OPEN");
            Map(m => m.High).Name("HIGH");
            Map(m => m.Low).Name("LOW");
            Map(m => m.Last).Name("LAST");
            Map(m => m.Settle).Name("SETTLE");
            Map(m => m.Change).Name("PT CHG");
            Map(m => m.Volume).Name("EST. VOL");
            Map(m => m.PriorSettle).Name("PRIOR SETTLE");
            Map(m => m.PriorVolume).Name("PRIOR VOL");
            Map(m => m.PriorOI).Name("PRIOR INT");
            Map(m => m.TradeDate).Name("TRADEDATE");
        }

    }
}
