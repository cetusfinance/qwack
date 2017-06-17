using System;
using System.Collections.Generic;
using System.Text;

namespace Qwack.Paths.PayoffScripting
{
    public abstract class PayoffProcess : IPathProcess
    {
        public void Process(PathBlock block)
        {
            throw new NotImplementedException();
        }

        public void SetupFeatures(FeatureCollection pathProcessFeaturesCollection)
        {
            throw new NotImplementedException();
        }
    }
}
