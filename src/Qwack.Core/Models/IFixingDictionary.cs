using System;
using System.Collections.Generic;
using System.Text;

namespace Qwack.Core.Models
{
    public interface IFixingDictionary : IDictionary<DateTime,double>
    {
        string Name { get; set; }
    }
}
