using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Transport.BasicTypes;

namespace Qwack.Core.Basic
{
    public class BasketDefinition
    {
        public Dictionary<string,double> BasketWeights { get; set; } 
        public BasketType BasketType { get; set; }
        public int NthModifier { get; set; } //for Nth BestOf/WorstOf use

        public Dictionary<string, string> FixingIndices { get; set; }

        public string Currency { get; set; }
        public CurrencyConversionType CurrencyConversionType { get; set; }
    }
}
