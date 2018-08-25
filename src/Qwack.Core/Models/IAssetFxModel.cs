using System;
using System.Collections.Generic;
using Qwack.Core.Basic;
using Qwack.Core.Curves;

namespace Qwack.Core.Models
{
    public interface IAssetFxModel
    {
        DateTime BuildDate { get; }
        IFundingModel FundingModel { get; }
     
        void AddPriceCurve(string name, IPriceCurve curve);
        void AddVolSurface(string name, IVolSurface surface);
        void AddFixingDictionary(string name, IDictionary<DateTime, double> fixings);

        double GetVolForStrikeAndDate(string name, DateTime expiry, double strike);

        IPriceCurve GetPriceCurve(string name);
        IVolSurface GetVolSurface(string name);
        IDictionary<DateTime, double> GetFixingDictionary(string name);
        bool TryGetFixingDictionary(string name, out IDictionary<DateTime, double> fixings);

        IAssetFxModel Clone();

        string[] CurveNames { get; }
    }
}
