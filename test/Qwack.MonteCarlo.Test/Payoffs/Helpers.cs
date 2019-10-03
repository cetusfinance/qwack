using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Moq;
using Qwack.Core.Models;
using Qwack.Paths;
using Qwack.Paths.Features;

namespace Qwack.MonteCarlo.Test.Payoffs
{
    public static class Helpers
    {
        private static int _nPaths = 128;

        public static Mock<IFeatureCollection> GetFeatureCollection()
        {
            var fc = new Mock<IFeatureCollection>();
            var pm = new Mock<IPathMappingFeature>();
            var ts = new Mock<ITimeStepsFeature>();
            var en = new Mock<IEngineFeature>();
            fc.Setup(c => c.GetFeature<IPathMappingFeature>()).Returns(pm.Object);
            fc.Setup(c => c.GetFeature<ITimeStepsFeature>()).Returns(ts.Object);
            fc.Setup(c => c.GetFeature<IEngineFeature>()).Returns(en.Object);
            pm.Setup(p => p.GetDimension("Asset")).Returns(0);
            pm.Setup(p => p.GetDimension("USD/ZAR")).Returns(1);
            ts.Setup(t => t.GetDateIndex(It.IsAny<DateTime>())).Returns(0);
            en.Setup(e => e.NumberOfPaths).Returns(_nPaths);

            return fc;
        }

        public static IPathBlock GetBlock(int nSteps)
        {
            var b = new PathBlock(_nPaths, 2, nSteps, 0);

            for (var i = 0; i < _nPaths; i += Vector<double>.Count)
            {
                var p = b.GetStepsForFactor(i, 0);
                for (var j = 0; j < nSteps; j++)
                {
                    p[j] = new Vector<double>(100.0);
                }
                var pfx = b.GetStepsForFactor(i, 1);
                for (var j = 0; j < nSteps; j++)
                {
                    pfx[j] = new Vector<double>(1.0);
                }
            }

            return b;
        }
    }
}
