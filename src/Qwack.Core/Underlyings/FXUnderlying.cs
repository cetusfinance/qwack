using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Dates;
using Qwack.Core.Curves;

namespace Qwack.Core.Underlyings
{
    class FXUnderlying : IUnderlying
    {
        public ICurve DomesticCurve { get; private set; }
        public ICurve ForeignCurve { get; private set; }
        public FXUnderlying(Frequency spotLag, Calendar holidays, DateTime originDate, double spot, ICurve domesticCurve, ICurve foreignCurve)
        {
            SpotLag = spotLag;
            Spot = spot;
            DomesticCurve = domesticCurve;
            ForeignCurve = foreignCurve;
            Holidays = holidays;
            OriginDate = originDate;
            SpotDate = OriginDate.AddPeriod(RollType.F, Holidays, SpotLag);
        }

        public Calendar Holidays { get; private set; }
        public Frequency SpotLag { get; private set; }
        public DateTime OriginDate { get; private set; }
        public DateTime SpotDate { get; private set; }
        public double Spot { get; private set; }

     
        public double GetForward(DateTime expiry)
        {
            var dfDom = DomesticCurve.GetDf(SpotDate, expiry);
            var dfFor = ForeignCurve.GetDf(SpotDate, expiry);
            var fwd = Spot * dfDom / dfFor;
            return fwd;
        }
    }
}
