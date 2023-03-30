using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qwack.Excel.Utils
{
    internal static class SessionItemExtensions
    {
        public const char SPLITCHAR = '¬';

        public static string StripVersion(this string name) => name.Split(SPLITCHAR)[0];
    }
}
