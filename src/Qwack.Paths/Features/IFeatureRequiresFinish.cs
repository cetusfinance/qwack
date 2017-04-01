using System;
using System.Collections.Generic;
using System.Text;

namespace Qwack.Paths.Features
{
    public interface IFeatureRequiresFinish
    {
        void Finish(FeatureCollection collection);
    }
}
