using System;
using System.Collections.Generic;
using System.Text;

namespace Qwack.Paths
{
    public interface IRequiresFinish
    {
        void Finish(FeatureCollection collection);
        bool IsComplete { get; }
    }
}
