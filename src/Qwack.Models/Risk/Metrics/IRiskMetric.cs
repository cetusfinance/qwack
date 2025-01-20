using System;
using System.Collections.Generic;
using Qwack.Core.Cubes;
using Qwack.Core.Models;

namespace Qwack.Models.Risk.Metrics
{
    public interface IRiskMetric : IDisposable
    {
        public Dictionary<string, IPvModel> GenerateScenarios();
        public ICube GenerateCubeFromResults(Dictionary<string, ICube> results, Dictionary<string, IPvModel> models);
    }
}
