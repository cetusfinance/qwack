using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Instruments.Asset;
using Qwack.Core.Models;
using Qwack.Dates;

namespace Qwack.Core.Instruments.Funding
{
    public class FxVanillaOption : IHasVega, IAssetInstrument
    {
        private readonly ICurrencyProvider _currencyProvider;
        private readonly ICalendarProvider _calendarProvider;

        public FxVanillaOption(ICurrencyProvider currencyProvider, ICalendarProvider calendarProvider)
        {
            _currencyProvider = currencyProvider;
            _calendarProvider = calendarProvider;
        }

        public OptionType CallPut { get; set; }
        public DateTime ExpiryDate { get; set; }

        public double Strike { get; set; }
        public double DomesticQuantity { get; set; }
        public DateTime DeliveryDate { get; set; }
        public string PortfolioName { get; set; }
        public Currency DomesticCCY { get; set; }
        public Currency ForeignCCY { get; set; }
        public Currency PaymentCurrency => ForeignCCY;
        public Currency Currency => PaymentCurrency;

        public string ForeignDiscountCurve { get; set; }

        public string TradeId { get; set; }
        public string Counterparty { get; set; }

        public string PairStr => $"{DomesticCCY.Ccy}/{ForeignCCY.Ccy}";
        public DateTime LastSensitivityDate => DeliveryDate;
        public string[] AssetIds => Array.Empty<string>();


        public Dictionary<string, Dictionary<DateTime, double>> Sensitivities(IFundingModel model)
        {
            var foreignCurve = model.FxMatrix.DiscountCurveMap[ForeignCCY];
            var domesticCurve = model.FxMatrix.DiscountCurveMap[DomesticCCY];
            var discountCurve = model.Curves[ForeignDiscountCurve];
            var df = discountCurve.Pv(1.0, DeliveryDate);
            var t = discountCurve.Basis.CalculateYearFraction(discountCurve.BuildDate, DeliveryDate);
            var fwdRate = model.GetFxRate(DeliveryDate, DomesticCCY, ForeignCCY);

            var domesticDict = new Dictionary<DateTime, double>() { { DeliveryDate, fwdRate * DomesticQuantity * df * t } };

            Dictionary<DateTime, double> foreignDict;

            if (foreignCurve == ForeignDiscountCurve)
            {
                foreignDict = new Dictionary<DateTime, double>() { { DeliveryDate, DomesticQuantity * df * (fwdRate * -2 * t + Strike * t) } };

                return new Dictionary<string, Dictionary<DateTime, double>>()
                {
                    {foreignCurve, foreignDict },
                    {domesticCurve, domesticDict },
                };
            }
            else
            {
                foreignDict = new Dictionary<DateTime, double>() { { DeliveryDate, fwdRate * DomesticQuantity * df * -t } };
                var foreignDiscDict = new Dictionary<DateTime, double>() { { DeliveryDate, (fwdRate - Strike) * DomesticQuantity * df * -t } };

                return new Dictionary<string, Dictionary<DateTime, double>>()
                {
                    {foreignCurve, foreignDict },
                    {domesticCurve, domesticDict },
                    {ForeignDiscountCurve, foreignDiscDict },
                };
            }
        }

        public IAssetInstrument Clone() => new FxVanillaOption(_currencyProvider, _calendarProvider)
        {
            CallPut = CallPut,
            Counterparty = Counterparty,
            DeliveryDate = DeliveryDate,
            DomesticCCY = DomesticCCY,
            DomesticQuantity = DomesticQuantity,
            ExpiryDate = ExpiryDate,
            ForeignCCY = ForeignCCY,
            ForeignDiscountCurve = ForeignDiscountCurve,
            Strike = Strike,
            TradeId = TradeId
        };

        public IAssetInstrument SetStrike(double strike) => throw new NotImplementedException();


        public string[] IrCurves(IAssetFxModel model) =>
            new[] {
                ForeignDiscountCurve,
                model.FundingModel.FxMatrix.DiscountCurveMap[ForeignCCY],
                model.FundingModel.FxMatrix.DiscountCurveMap[DomesticCCY] }
            .Distinct()
            .ToArray();

        public Dictionary<string, List<DateTime>> PastFixingDates(DateTime valDate) => new Dictionary<string, List<DateTime>>();

        public FxConversionType FxType(IAssetFxModel model) => FxConversionType.None;

        public string FxPair(IAssetFxModel model) => PairStr;

        public FxPair Pair => new FxPair()
        {
            Domestic = DomesticCCY,
            Foreign = ForeignCCY,
            SettlementCalendar = _calendarProvider.Collection[PairStr.Replace('/', '+')],
            SpotLag = 2.Bd()
        };

        private bool InTheMoney(IAssetFxModel model) =>
            CallPut == OptionType.Call ?
                (model.FundingModel.GetFxRate(DeliveryDate, DomesticCCY, ForeignCCY) > Strike) :
                (model.FundingModel.GetFxRate(DeliveryDate, DomesticCCY, ForeignCCY) < Strike);

        public List<CashFlow> ExpectedCashFlows(IAssetFxModel model) => !InTheMoney(model) ? new List<CashFlow>() : new List<CashFlow>
            {
                new CashFlow()
                {
                    Currency = DomesticCCY,
                    SettleDate = DeliveryDate,
                    Notional = DomesticQuantity,
                    Fv = DomesticQuantity
                },
                new CashFlow()
                {
                    Currency = ForeignCCY,
                    SettleDate = DeliveryDate,
                    Notional = DomesticQuantity * Strike,
                    Fv = DomesticQuantity * Strike
                }
            };
            
    }
}
