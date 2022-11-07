using System;
using System.Collections.Generic;
using System.Text;

namespace Qwack.Transport.Results
{
    public class StressTestResult
    {
        public string Id { get; set; }
        public double StressSize { get; set; }
        public Dictionary<double, double> ScenarioPoints { get; set; }
        public LinearRegressionResult LR { get; set; }
        public decimal StressPvChange { get; set; }

    }
}
