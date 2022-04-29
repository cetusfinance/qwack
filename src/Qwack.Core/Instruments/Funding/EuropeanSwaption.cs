using System;
using Qwack.Core.Basic;
using Qwack.Core.Basic.Capital;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Transport.BasicTypes;

namespace Qwack.Core.Instruments.Funding
{
    public class EuropeanSwaption : IrSwap, IHasVega, ISaCcrEnabledIR
    {
        private readonly ICurrencyProvider _currencyProvider;
        private readonly ICalendarProvider _calendarProvider;

        public EuropeanSwaption(ICurrencyProvider currencyProvider, ICalendarProvider calendarProvider)
        {
            _currencyProvider = currencyProvider;
            _calendarProvider = calendarProvider;
        }

        public EuropeanSwaption(DateTime startDate, Frequency swapTenor, FloatRateIndex rateIndex, double strike,
            SwapPayReceiveType swapType, string forecastCurve, string discountCurve, DateTime expiryDate)
            : base(startDate, swapTenor, rateIndex, strike, swapType, forecastCurve, discountCurve)
        {
            CallPut = swapType == SwapPayReceiveType.Pay ? OptionType.P : OptionType.C;
            ExpiryDate = expiryDate;
            Strike = strike;
        }

        public OptionType CallPut { get; set; }
        public DateTime ExpiryDate { get; set; }
        public double Strike { get; set; }

        public override double SupervisoryDelta(IAssetFxModel model) => SaCcrUtils.SupervisoryDelta(CalculateParRate(model.FundingModel), Strike, T(model), CallPut, SupervisoryVol, (SwapType == SwapPayReceiveType.Pay ? 1.0 : -1.0) * System.Math.Sign(Notional));
        public override double EffectiveNotional(IAssetFxModel model, double? MPOR = null) => SupervisoryDelta(model) * AdjustedNotional(model) * MaturityFactor(model.BuildDate, MPOR);
        private double SupervisoryVol => SaCcrParameters.SupervisoryOptionVols[SaCcrAssetClass.InterestRate];
        private double T(IAssetFxModel model) => model.BuildDate.CalculateYearFraction(ExpiryDate, DayCountBasis.Act365F);

    }
}
