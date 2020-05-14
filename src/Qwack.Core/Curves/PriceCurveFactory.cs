using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
                }; 

            if (transportObject.BasicPriceCurve != null)
                return new BasicPriceCurve(transportObject.BasicPriceCurve, currencyProvider);

            if (transportObject.BasisPriceCurve != null)
                return new BasisPriceCurve(transportObject.BasisPriceCurve, currencyProvider, calendarProvider);

            if (transportObject.ContangoPriceCurve != null)
                return new ContangoPriceCurve(transportObject.ContangoPriceCurve, currencyProvider);


            throw new Exception("Unable to build price curve");
        }

        public static TO_PriceCurve GetTransportObject(this IPriceCurve curve)
        {
            switch(curve)
            {
                case ConstantPriceCurve cons:
                    return new TO_PriceCurve
                    {
                        ConstantPriceCurve = new TO_ConstantPriceCurve
                        {
                            AssetId = cons.AssetId,
                            BuildDate = cons.BuildDate,
                            Currency = cons.Currency.Ccy,
                            Name = cons.Name,
                            Price = cons.Price,
                        }
                    };
                case BasicPriceCurve basic:
                    return new TO_PriceCurve
                    {
                        BasicPriceCurve = new TO_BasicPriceCurve
                        {
                            AssetId = basic.AssetId,
                            BuildDate = basic.BuildDate,
                            CollateralSpec = basic.CollateralSpec,
                            Currency = basic.Currency.Ccy,
                            CurveType =basic.CurveType,
                            Name = basic.Name ,
                            PillarDates = basic.PillarDates,
                            PillarLabels = basic.PillarLabels,
                            Prices = basic.Prices,
                            SpotCalendar = basic.SpotCalendar?.Name,
                            SpotLag = basic.SpotLag.ToString()
                        }
                    };
                case BasisPriceCurve basis:
                    return new TO_PriceCurve
                    {
                        BasisPriceCurve = new TO_BasisPriceCurve
                        {
                            AssetId = basis.AssetId,
                            Currency = basis.Currency.Ccy,
                            Name = basis.Name,
                            SpotCalendar = basis.SpotCalendar.Name,
                            SpotLag = basis.SpotLag.ToString(),
                            BaseCurve = basis.BaseCurve.GetTransportObject(),
                            BuildDate = basis.BuildDate,
                            Curve = basis.Curve.GetTransportObject(),
                            CurveType = basis.CurveType,
                            DiscountCurve = ((IrCurve)basis.DiscountCurve).GetTransportObject(),
                            Instruments = basis.Instruments.Select(x=>x.GetTransportObject()).ToList(),
                            PillarLabels = basis.PillarLabels,
                            Pillars = basis.Pillars
                        }
                    };
                case ContangoPriceCurve c:
                    return new TO_PriceCurve
                    {
                        ContangoPriceCurve = new TO_ContangoPriceCurve
                        {
                            AssetId = c.AssetId,
                            Currency = c.Currency.Ccy,
                            Name = c.Name,
                            SpotCalendar = c.SpotCalendar.Name,
                            SpotLag = c.SpotLag.ToString(),
                            BuildDate = c.BuildDate,
                            PillarLabels = c.PillarLabels,
                            PillarDates = c.PillarDates,
                            Basis = c.Basis,
                            Contangos = c.Contangos,
                            Spot = c.Spot,
                            SpotDate = c.SpotDate
                        }
                    };
                default:
                    throw new Exception($"Unable to serialize price curve of type {curve.GetType()}");
            }
        }
    }
}
