using System;
using System.Collections.Generic;
using System.Text;

namespace Qwack.Core.Models
{
    public interface IRequiresFinish
    {
        void Finish(IFeatureCollection collection);
        bool IsComplete { get; }
    }
}
