using System;
using System.Collections.Generic;
using System.Text;

namespace Qwack.Core.Basic
{
    public class BasketDefinition
    {
        public Dictionary<string,double> BasketWeights { get; set; } 
        public BasketType BasketType { get; set; }
        public int NthModifier { get; set; } //for Nth BestOf/WorstOf use
    }
}
