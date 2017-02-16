using System;
using System.Collections.Generic;
using System.Text;

namespace Qwack.Paths
{
    public interface IPathProcess
    {
        void SetupFeatures(List<object> pathProcessFeaturesCollection);
        void Process(PathBlock block);
    }
}
