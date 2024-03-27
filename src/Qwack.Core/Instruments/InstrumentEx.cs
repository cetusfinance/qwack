using System;
using Qwack.Core.Instruments.Asset;
using Qwack.Core.Instruments.Funding;
using Qwack.Transport.BasicTypes;
using Qwack.Transport.TransportObjects.Instruments;

namespace Qwack.Core.Instruments
{
    public static class InstrumentEx
    {
        public static TO_Instrument GetTransportObject(this IInstrument instrument) => instrument switch
        {
            AsianOption asianOption => asianOption.ToTransportObject(),
            AsianSwap asianSwap => asianSwap.ToTransportObject(),
            AsianSwapStrip asianSwapStrip => asianSwapStrip.ToTransportObject(),
            AsianBasisSwap asianBasisSwap => asianBasisSwap.ToTransportObject(),
            EuropeanOption europeanOption => europeanOption.ToTransportObject(),
            Forward forward => forward.ToTransportObject(),
            Equity equity => equity.ToTransportObject(),
            Bond bond => bond.ToTransportObject(),
            CashBalance cashBalance => new TO_Instrument { FundingInstrumentType = FundingInstrumentType.CashBalance, CashBalance = cashBalance.ToTransportObject() },
            FuturesOption futuresOption => futuresOption.ToTransportObject(),
            FxFuture fxFuture => fxFuture.ToTransportObject(),
            FxVanillaOption fxo => fxo.ToTransportObject(),
            FxForward fxForward => fxForward.ToTransportObject(),
            FxPerpetual fxPerpetual => fxPerpetual.ToTransportObject(),
            Future future => future.ToTransportObject(),
            CashWrapper cashWrapper => cashWrapper.ToTransportObject(),
            InflationPerformanceSwap inflationPerformanceSwap => inflationPerformanceSwap.ToTransportObject(),
            IrSwap irs => irs.ToTransportObject(),
            AssetTrs trs => trs.ToTransportObject(),
            SyntheticCashAndCarry cnc => cnc.ToTransportObject(),
            _ => throw new Exception("Unable to serialize instrument"),
        };
    }
}
