using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Qwack.Random
{
    public class RandomOptions
    {
        public int TotalPaths { get; set; }
        public int BlockSize { get; set; }
        public int StartPath { get; set; }
        public int Seed { get; set; }
        public int Factors { get; set; }
        public int TimeSteps { get; set; }
        public bool Normal { get; set; }
    }
}
