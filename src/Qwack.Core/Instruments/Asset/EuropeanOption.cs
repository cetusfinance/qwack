using System;
using System.Collections.Generic;
using System.Text;
using Qwack.Core.Basic;
using Qwack.Core.Curves;
using Qwack.Core.Models;
using Qwack.Dates;
using Qwack.Transport.BasicTypes;
using Qwack.Transport.TransportObjects.Instruments;
using Qwack.Transport.TransportObjects.Instruments.Asset;

namespace Qwack.Core.Instruments.Asset
{
    public class EuropeanOption : Forward, IHasVega, ISaCcrEnabled
    {
        public OptionType CallPut { get; set; }

        public new IAssetInstrument Clone() => new EuropeanOption
        {
            TradeId = TradeId,
            Notional = Notional,
            Direction = Direction,
            ExpiryDate = ExpiryDate,
            FixingCalendar = FixingCalendar,
            PaymentCalendar = PaymentCalendar,
            SpotLag = SpotLag,
            PaymentLag = PaymentLag,
            Strike = Strike,
            AssetId = AssetId,
            PaymentCurrency = PaymentCurrency,
            FxFixingId = FxFixingId,
            DiscountCurve = DiscountCurve,
            PaymentDate = PaymentDate,
            Counterparty = Counterparty,
            FxConversionType = FxConversionType,
            HedgingSet = HedgingSet,
            PortfolioName = PortfolioName,
            CallPut = CallPut
        };

        public new IAssetInstrument SetStrike(double strike)
        {
            var o = (EuropeanOption)Clone();
            o.Strike = strike;
            return o;
        }

        public override bool Equals(object obj) => obj is EuropeanOption euroOpt &&
                   CallPut == euroOpt.CallPut &&
                   base.Equals(euroOpt);


        public override double SupervisoryDelta(IAssetFxModel model) => SaCcrUtils.SupervisoryDelta(Fwd(model), Strike, T(model), CallPut, SupervisoryVol, Notional);
        public override double EffectiveNotional(IAssetFxModel model) => SupervisoryDelta(model) * AdjustedNotional(model) * MaturityFactor(model.BuildDate);
        private double SupervisoryVol => HedgingSet == "Electricity" ? 1.50 : 0.70;
        private double T(IAssetFxModel model) => model.BuildDate.CalculateYearFraction(ExpiryDate, DayCountBasis.Act365F);
        public new TO_Instrument ToTransportObject()
        {
            var fwdTO = base.ToTransportObject().Forward;
            var eoTO = (TO_EuropeanOption)fwdTO;
            eoTO.CallPut = CallPut;
            return new TO_Instrument
            {
                AssetInstrumentType = AssetInstrumentType.EuropeanOption,
                EuropeanOption = eoTO
            };
        }
    }
}
