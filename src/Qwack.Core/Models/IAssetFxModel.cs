using System;
using Qwack.Core.Basic;
using Qwack.Core.Curves;

namespace Qwack.Core.Models
{
    public interface IAssetFxModel
    {
        DateTime BuildDate { get; }
        IFundingModel FundingModel { get; }
        object TurnbullWakeman { get; }

        void AddPriceCurve(string name, IPriceCurve curve);
        void AddVolSurface(string name, IVolSurface surface);
        IPriceCurve GetPriceCurve(string name);
        IVolSurface GetVolSurface(string name);
    }
}
