using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Core.Models;

namespace Qwack.Models.Models
{
    public class FixingDictionary : Dictionary<DateTime, double>, IFixingDictionary
    {
        public string Name { get; set; }
    }
}
