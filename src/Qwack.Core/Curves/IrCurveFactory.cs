using System;
using Qwack.Core.Basic;
using Qwack.Dates;
using Qwack.Transport.TransportObjects.MarketData.Curves;

namespace Qwack.Core.Curves
{
    public static class IrCurveFactory
    {
        public static IIrCurve GetCurve(TO_IIrCurve to, ICurrencyProvider currencyProvider, ICalendarProvider calendarProvider)
        {
            if (to.IrCurve != null)
                return (IIrCurve)new IrCurve(to.IrCurve, currencyProvider);
            else if (to.SeasonalCpiCurve != null)
                return (IIrCurve)new SeasonalCpiCurve(to.SeasonalCpiCurve, calendarProvider);
            else if (to.CPICurve != null)
                return (IIrCurve)new CPICurve(to.CPICurve, calendarProvider);

            throw new NotImplementedException("Unable to deserialize IR curve from transport object");
        }

        public static TO_IIrCurve GetTransportObject(this IIrCurve curve)
        {
            if (curve is IrCurve ir)
                return new TO_IIrCurve { IrCurve = ir.GetTransportObject() };
            else if (curve is SeasonalCpiCurve scpi)
                return new TO_IIrCurve { SeasonalCpiCurve = scpi.GetTransportObject() };
            else if (curve is CPICurve cpi)
                return new TO_IIrCurve { CPICurve = cpi.GetTransportObject() };
            return null;
        }
    }
}
