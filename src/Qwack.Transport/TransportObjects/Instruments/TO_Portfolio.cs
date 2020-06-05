using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;

namespace Qwack.Transport.TransportObjects.Instruments
{
    [ProtoContract]
    public class TO_Portfolio
    {
        [ProtoMember(2)]
        public List<TO_Instrument> Instruments { get; set; }
        [ProtoMember(3)]
        public string PortfolioName { get; set; }



        public override bool Equals(object obj) => obj is TO_Portfolio portfolio &&
                   EqualityComparer<List<TO_Instrument>>.Default.Equals(Instruments, portfolio.Instruments) &&
                   PortfolioName == portfolio.PortfolioName;

        public override int GetHashCode()
        {
            var hashCode = 4817385;
            hashCode = hashCode * -1521134295 + EqualityComparer<List<TO_Instrument>>.Default.GetHashCode(Instruments);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(PortfolioName);
            return hashCode;
        }
    }
}
