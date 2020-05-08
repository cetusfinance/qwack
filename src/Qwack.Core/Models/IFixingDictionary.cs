using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Transport.BasicTypes;
using Qwack.Transport.TransportObjects.MarketData.Models;

namespace Qwack.Core.Models
{
    public interface IFixingDictionary : IDictionary<DateTime, double>
    {
        string Name { get; set; }
        string AssetId { get; set; }
        string FxPair { get; set; }
        FixingDictionaryType FixingDictionaryType { get; set; }
        IFixingDictionary Clone();
        double GetFixing(DateTime d);
        bool TryGetFixing(DateTime d, out double fixing);
        TO_FixingDictionary GetTransportObject();

    }
}
