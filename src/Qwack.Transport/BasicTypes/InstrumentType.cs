using System;
using System.Collections.Generic;
using System.Text;

namespace Qwack.Transport.BasicTypes
{
    public enum AssetInstrumentType
    {
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

        None
    }

    public enum FundingInstrumentType
    {
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

        None
    }
}
