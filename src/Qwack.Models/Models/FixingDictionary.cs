using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Models;
namespace Qwack.Models.Models
{
    public class FixingDictionary : Dictionary<DateTime, double>, IFixingDictionary
    {
        public string Name { get; set; }
        public string AssetId { get; set; }
        public string FxPair { get; set; }
        public FixingDictionaryType FixingDictionaryType { get; set; }
        public IFixingDictionary Clone()
        {
            var o = new FixingDictionary() { AssetId = AssetId, Name = Name, FixingDictionaryType = FixingDictionaryType, FxPair = FxPair };
            foreach (var kv in this)
            {
                o.Add(kv.Key, kv.Value);
            }
            return o;
        }

        public double GetFixing(DateTime d)
        {
            return TryGetFixing(d, out var fixing) ? fixing : throw new Exception($"Fixing for date {d:yyyy-MM-dd} not found in dictionary {Name}");
        }

        public bool TryGetFixing(DateTime d, out double fixing) => TryGetValue(d, out fixing);
    }
}
