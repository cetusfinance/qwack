using System;
using System.Collections.Generic;
using System.Text;

namespace Qwack.Transport.BasicTypes
{
    public enum FundingInstrumentType
    {
        None,

        CashBalance,
        ContangoSwap,
        FixedRateLoanDeposit,
        FloatingRateLoanDepo,
        ForwardRateAgreement,
        FxForward,
        FxPerpetual,
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
