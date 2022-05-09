using System.Collections.Concurrent;
using System.IO;
using System.IO.Compression;
using System.Xml.Serialization;
using Qwack.Transport.CmeXml;

namespace Qwack.Providers.CSV
{
    public class COMEXParser
    {
        public FIXML GetBlob(string filename)
        {
            if (_blobCache.TryGetValue(filename, out var record))
                return record;

            var reader = new XmlSerializer(typeof(FIXML));
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

        private static readonly COMEXParser instance = new();

        static COMEXParser()
        {
        }

        private COMEXParser()
        {
        }

        public static COMEXParser Instance => instance;

        private readonly ConcurrentDictionary<string, FIXML> _blobCache = new();
    }
}
