using System;
using System.Collections.Generic;
using Qwack.Core.Basic;
using Qwack.Core.Basic.Capital;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Transport.BasicTypes;
using Qwack.Transport.TransportObjects.Instruments;
using Qwack.Transport.TransportObjects.Instruments.Funding;

namespace Qwack.Core.Instruments.Funding
{
    public class FxVanillaOption : FxForward, IHasVega, IAssetInstrument
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
        public double Premium { get; set; }
        public DateTime PremiumDate { get; set; }

        public override double SupervisoryDelta(IAssetFxModel model) => SaCcrUtils.SupervisoryDelta(model.FundingModel.GetFxRate(ExpiryDate, Pair), Strike, T(model), CallPut, SupervisoryVol, DomesticQuantity);
        public override double EffectiveNotional(IAssetFxModel model, double? MPOR = null) => SupervisoryDelta(model) * AdjustedNotional(model) * MaturityFactor(model.BuildDate, MPOR);
        private double SupervisoryVol => SaCcrParameters.SupervisoryOptionVols[SaCcrAssetClass.Fx];
        private double T(IAssetFxModel model) => model.BuildDate.CalculateYearFraction(ExpiryDate, DayCountBasis.Act365F);

        public override IAssetInstrument Clone() => new FxVanillaOption(_currencyProvider, _calendarProvider)
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
            TradeId = TradeId,
            PremiumDate = PremiumDate,
            Premium = Premium,
        };

        private bool InTheMoney(IAssetFxModel model) =>
            CallPut == OptionType.Call ?
                (model.FundingModel.GetFxRate(DeliveryDate, DomesticCCY, ForeignCCY) > Strike) :
                (model.FundingModel.GetFxRate(DeliveryDate, DomesticCCY, ForeignCCY) < Strike);

        public override List<CashFlow> ExpectedCashFlows(IAssetFxModel model) => !InTheMoney(model) ? new List<CashFlow>() : new List<CashFlow>
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

        public override TO_Instrument ToTransportObject() =>
         new()
         {
             FundingInstrumentType = FundingInstrumentType.FxVanillaOption,
             FxOption = new TO_FxVanillaOption
             {
                 TradeId = TradeId,
                 DomesticQuantity = DomesticQuantity,
                 DomesticCCY = DomesticCCY,
                 ForeignCCY = ForeignCCY,
                 ForeignDiscountCurve = ForeignDiscountCurve,
                 DeliveryDate = DeliveryDate,
                 PillarDate = PillarDate,
                 SolveCurve = SolveCurve,
                 Strike = Strike,
                 CallPut = CallPut,
                 ExerciseType = OptionExerciseType.European,
                 Premium = Premium,
                 PremiumDate = PremiumDate,
                 ExpiryDate = ExpiryDate,
                 Counterparty = Counterparty,
                 HedgingSet = HedgingSet,
                 PortfolioName = PortfolioName,
                 MetaData = new(MetaData)
             }
         };
    }
}
