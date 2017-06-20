using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qwack.Futures
{
    public class FutureCode
    {
        public string Code { get; set; }
        public int Year { get; set; } 
        public string YearCode { get; set; }
        public string MonthCode { get; set; }
        public int Month { get; set; }
        public string ContractCode { get; set; }
        public string Prefix { get; set; }
        public string PostFix { get; set; }
    }
}
