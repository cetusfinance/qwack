using System;
using System.Linq;
using Qwack.Core.Basic;
using Qwack.Core.Instruments;
using Qwack.Dates;
using Qwack.Transport.TransportObjects.MarketData.Curves;

namespace Qwack.Core.Curves
{
    public static class PriceCurveFactory
    {
        public static IPriceCurve GetPriceCurve(this TO_PriceCurve transportObject, ICurrencyProvider currencyProvider, ICalendarProvider calendarProvider)
        {
            if (transportObject.ConstantPriceCurve != null)
                return new ConstantPriceCurve(transportObject.ConstantPriceCurve.Price, transportObject.ConstantPriceCurve.BuildDate, currencyProvider)
                {
                    Currency = currencyProvider.GetCurrencySafe(transportObject.ConstantPriceCurve.Currency),
                    AssetId = transportObject.ConstantPriceCurve.AssetId,
                    Name = transportObject.ConstantPriceCurve.Name,
                    Units = transportObject.ConstantPriceCurve.Units,
                };

            if (transportObject.BasicPriceCurve != null)
                return new BasicPriceCurve(transportObject.BasicPriceCurve, currencyProvider);

            if (transportObject.BasisPriceCurve != null)
                return new BasisPriceCurve(transportObject.BasisPriceCurve, currencyProvider, calendarProvider);

            if (transportObject.ContangoPriceCurve != null)
                return new ContangoPriceCurve(transportObject.ContangoPriceCurve, currencyProvider);


            throw new Exception("Unable to build price curve");
        }

        public static TO_PriceCurve GetTransportObject(this IPriceCurve curve) => curve switch
        {
            ConstantPriceCurve cons => new TO_PriceCurve
            {
                ConstantPriceCurve = new TO_ConstantPriceCurve
                {
                    AssetId = cons.AssetId,
                    BuildDate = cons.BuildDate,
                    Currency = cons.Currency,
                    Name = cons.Name,
                    Price = cons.Price,
                }
            },
            BasicPriceCurve basic => new TO_PriceCurve
            {
                BasicPriceCurve = new TO_BasicPriceCurve
                {
                    AssetId = basic.AssetId,
                    BuildDate = basic.BuildDate,
                    CollateralSpec = basic.CollateralSpec,
                    Currency = basic.Currency,
                    CurveType = basic.CurveType,
                    Name = basic.Name,
                    PillarDates = basic.PillarDates,
                    PillarLabels = basic.PillarLabels,
                    Prices = basic.Prices,
                    SpotCalendar = basic.SpotCalendar?.Name,
                    SpotLag = basic.SpotLag.ToString()
                }
            },
            BasisPriceCurve basis => new TO_PriceCurve
            {
                BasisPriceCurve = new TO_BasisPriceCurve
                {
                    AssetId = basis.AssetId,
                    Currency = basis.Currency.Ccy,
                    Name = basis.Name,
                    SpotCalendar = basis.SpotCalendar?.Name,
                    SpotLag = basis.SpotLag.ToString(),
                    BaseCurve = basis.BaseCurve.GetTransportObject(),
                    BuildDate = basis.BuildDate,
                    Curve = basis.Curve.GetTransportObject(),
                    CurveType = basis.CurveType,
                    DiscountCurve = ((IrCurve)basis.DiscountCurve).GetTransportObject(),
                    Instruments = basis.Instruments.Select(x => x.GetTransportObject()).ToList(),
                    PillarLabels = basis.PillarLabels,
                    Pillars = basis.Pillars
                }
            },
            ContangoPriceCurve c => new TO_PriceCurve
            {
                ContangoPriceCurve = new TO_ContangoPriceCurve
                {
                    AssetId = c.AssetId,
                    Currency = c.Currency,
                    Name = c.Name,
                    SpotCalendar = c.SpotCalendar?.Name,
                    SpotLag = c.SpotLag.ToString(),
                    BuildDate = c.BuildDate,
                    PillarLabels = c.PillarLabels,
                    PillarDates = c.PillarDates,
                    Basis = c.Basis,
                    Contangos = c.Contangos,
                    Spot = c.Spot,
                    SpotDate = c.SpotDate
                }
            },
            EquityPriceCurve e => new TO_PriceCurve
            {
                EquityPriceCurve = new TO_EquityPriceCurve
                {
                    AssetId = e.AssetId,
                    Currency = e.Currency,
                    Name = e.Name,
                    SpotCalendar = e.SpotCalendar?.Name,
                    SpotLag = e.SpotLag.ToString(),
                    BuildDate = e.BuildDate,
                    PillarLabels = e.PillarLabels,
                    PillarDates = e.PillarDates,
                    Basis = e.Basis,
                    DiscreteDivDates = e.DiscreteDivDates,
                    DiscreteDivs = e.DiscreteDivs,
                    DivYields = e.DivYields,
                    Units = e.Units,
                    Spot = e.Spot,
                    SpotDate = e.SpotDate
                }
            },
            _ => throw new Exception($"Unable to serialize price curve of type {curve.GetType()}"),
        };
    }
}
