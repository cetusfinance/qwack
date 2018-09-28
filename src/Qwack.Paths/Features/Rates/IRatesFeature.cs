using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Core.Models;

namespace Qwack.Paths.Features.Rates
{
    public interface IRatesFeature: IRequiresFinish
    {
        IRateFeature GetRate(string name);
        void AddRate(IRateFeature feature);
    }
}
