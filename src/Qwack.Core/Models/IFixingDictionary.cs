using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Core.Basic;
namespace Qwack.Core.Models
{
    public interface IFixingDictionary : IDictionary<DateTime, double>
    {
        string Name { get; set; }
        string AssetId { get; set; }
        string FxPair { get; set; }
        FixingDictionaryType FixingDictionaryType { get; set; }
        IFixingDictionary Clone();
    }
}
