using System;
using Qwack.Core.Models;

namespace Qwack.Paths.PayoffScripting
{
    public abstract class PayoffProcess : IPathProcess
    {
        public void Process(IPathBlock block) => throw new NotImplementedException();

        public void SetupFeatures(IFeatureCollection pathProcessFeaturesCollection) => throw new NotImplementedException();
    }
}
