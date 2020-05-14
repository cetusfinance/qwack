using System;
using System.Collections.Generic;
using System.Text;

namespace Qwack.Transport.BasicTypes
{
    public enum AssetInstrumentType
    {
        None,

        AsianSwap,
        AsianSwapStrip,
        AsianOption,
        AsianLookbackOption,
        AsianBasisSwap,
        BackPricingOption,
        DoubleNoTouchOption,
        ETC,
        EuropeanBarrierOption,
        EuropeanOption,
        Forward,
        Futrure,
        FuturesOption,
        MultiPeriodBackpricingOption,
        OneTouchOption,

    }

    public enum FundingInstrumentType
    {
        None,

        CashBalance,
        ContangoSwap,
        FixedRateLoanDeposit,
        FloatingRateLoanDepo,
        ForwardRateAgreement,
        FxForward,
        FxSwap,
        FxVanillaOption,
        IrBasisSwap,
        IrSwap,
        OISFuture,
        PhysicalBalance,
        STIRFuture,
        XccyBasisSwap,
        ZeroBond,
    }
}
