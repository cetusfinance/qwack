using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qwack.Transport.BasicTypes;

namespace Qwack.Dates
{
    /// <summary>
    /// Relative date implementation of the ITenorDate interface
    /// </summary>
    public class TenorDateRelative : ITenorDate
    {
        public TenorDateRelative(Frequency relativeTenor) => RelativeTenor = relativeTenor;

        public Frequency RelativeTenor { get; set; }

        public DateTime Date(DateTime refDate, RollType rollType, Calendar calendars) => refDate.AddPeriod(rollType, calendars, RelativeTenor);
        public override bool Equals(object obj) => obj is TenorDateRelative relative && EqualityComparer<Frequency>.Default.Equals(RelativeTenor, relative.RelativeTenor);
    }
}
