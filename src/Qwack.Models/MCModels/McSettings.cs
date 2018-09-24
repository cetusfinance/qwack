using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Core.Basic;

namespace Qwack.Models.MCModels
{
    public class McSettings
    {
        public int NumberOfPaths { get; set; }
        public int NumberOfTimesteps { get; set; }
        public RandomGeneratorType Generator { get; set; }
    }
}
