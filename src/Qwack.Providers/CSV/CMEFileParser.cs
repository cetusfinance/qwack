using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Serialization;
using CsvHelper;
using CsvHelper.Configuration;
using Qwack.Transport.CmeXml;

namespace Qwack.Providers.CSV
{
    public class CMEFileParser
    {
        private readonly ConcurrentDictionary<string, List<CMEFileRecord>> _cache = new();
        private readonly ConcurrentDictionary<string, FIXML> _blobCache = new();

        public List<CMEFileRecord> Parse(string fileName)
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

            var list = csv.GetRecords<CMEFileRecord>().ToList();
            _cache.TryAdd(fileName, list);

            return list;
        }

        public List<CMEFileRecord> Parse(StreamReader stream)
        {
            using var csv = new CsvReader(stream, CultureInfo.InvariantCulture);
            csv.Context.Configuration.HasHeaderRecord = true;
            var list = csv.GetRecords<CMEFileRecord>().ToList();
            return list;
        }

        public FIXML GetBlob(string filename)
        {
            if (_blobCache.TryGetValue(filename, out var record))
                return record;

            XmlSerializer reader = null;

            try
            {
                reader = new XmlSerializer(typeof(FIXML));
            }
            catch (FileNotFoundException)
            {

            }

            var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            FIXML blob;
            if (filename.EndsWith(".gz"))
            {
                var gs = new GZipStream(fs, CompressionMode.Decompress);
                blob = (FIXML)reader.Deserialize(gs);
            }
            else
            {
                blob = (FIXML)reader.Deserialize(fs);
            }
            _blobCache.TryAdd(filename, blob);
            return blob;
        }

        public FIXML GetBlob(StreamReader stream)
        {
            XmlSerializer reader = null;

            try
            {
                reader = new XmlSerializer(typeof(FIXML));
            }
            catch (FileNotFoundException)
            {

            }

            var blob = (FIXML)reader.Deserialize(stream);
            return blob;
        }

        private static readonly CMEFileParser instance = new();

        static CMEFileParser()
        {
        }

        private CMEFileParser()
        {
        }

        public static CMEFileParser Instance => instance;
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
