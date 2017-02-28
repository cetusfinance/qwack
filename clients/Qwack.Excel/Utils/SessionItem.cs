using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qwack.Excel.Utils
{
    public class SessionItem <T>: ISessionItem
    {
        public string Name { get; set; }

        public int Version { get; set; }

        public T Value { get; set; }
    }
}
