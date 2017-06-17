using Qwack.Paths.Features;
using System;
using System.Collections.Generic;
using System.Text;

namespace Qwack.Paths.PayoffScripting
{
    public class ScriptedPayoffGenerator : IPathProcess
    {
        public string[][] Cashflows { get; set; }
        public string[] CashflowTitles { get; set; }
        public string[] CashflowCurrencies { get; set; }
        public DateTime[] EventDates { get; set; }
        public string[] Underlyings { get; set; }
        public string[] Aliases { get; set; }
        public string[][] Expresssions { get; set; }

        public void Process(PathBlock block) => throw new NotImplementedException();

        public void SetupFeatures(FeatureCollection features)
        {
            var dates = features.GetFeature<ITimeStepsFeature>();
            dates.AddDates(EventDates);
        }
    }
}
