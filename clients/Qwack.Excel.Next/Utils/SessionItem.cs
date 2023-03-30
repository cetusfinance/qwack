using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qwack.Excel.Utils
{
    public class SessionItem <T>: ISessionItem<T>
    {
        public string Name { get; set; }
        public int Version { get; set; }
        public T Value { get; set; }

        public override string ToString() => $"{Name}|{Version}";
    }
}
