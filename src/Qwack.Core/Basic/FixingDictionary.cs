using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Qwack.Core.Models;
using Qwack.Transport.BasicTypes;
using Qwack.Transport.TransportObjects.MarketData.Models;

namespace Qwack.Core.Basic
{
    public class FixingDictionary : Dictionary<DateTime, double>, IFixingDictionary
    {
        public string Name { get; set; }
        public string AssetId { get; set; }
        public string FxPair { get; set; }
        public FixingDictionaryType FixingDictionaryType { get; set; }

        public FixingDictionary() { }
        public FixingDictionary(Dictionary<DateTime, double> source) :
            base(source)
        {
        }
        public FixingDictionary(IFixingDictionary source) :
            base(source)
        {
        }
        public FixingDictionary(TO_FixingDictionary transportObject) 
            : base(transportObject.Fixings==null?new Dictionary<DateTime, double>():transportObject.Fixings.ToDictionary(x=>DateTime.ParseExact(x.Key, "s",CultureInfo.InvariantCulture), x=>x.Value))
        {
            Name = transportObject.Name;
            AssetId = transportObject.AssetId;
            FxPair = transportObject.FxPair;
            FixingDictionaryType = transportObject.FixingDictionaryType;
        }

        public IFixingDictionary Clone()
        {
            var o = new FixingDictionary() { AssetId = AssetId, Name = Name, FixingDictionaryType = FixingDictionaryType, FxPair = FxPair };
            foreach (var kv in this)
            {
                o.Add(kv.Key, kv.Value);
            }
            return o;
        }

        public double GetFixing(DateTime d) => TryGetFixing(d, out var fixing) ? fixing : throw new Exception($"Fixing for date {d:yyyy-MM-dd} not found in dictionary {Name}");

        public bool TryGetFixing(DateTime d, out double fixing) => TryGetValue(d, out fixing);

        public TO_FixingDictionary GetTransportObject() =>
            new()
            {
                AssetId = AssetId,
                Name = Name,
                FxPair = FxPair,
                FixingDictionaryType = FixingDictionaryType,
                Fixings = this.ToDictionary(x=>x.Key.ToString("s"),x=>x.Value)
            };
    }
}
