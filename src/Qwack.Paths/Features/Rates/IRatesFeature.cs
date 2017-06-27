using System;
using System.Collections.Generic;
using System.Text;

namespace Qwack.Paths.Features.Rates
{
    public interface IRatesFeature: IRequiresFinish
    {
        IRateFeature GetRate(string name);
        void AddRate(IRateFeature feature);
    }
}
