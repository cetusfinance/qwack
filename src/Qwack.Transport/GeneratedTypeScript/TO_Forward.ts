
import { TradeDirection } from './TradeDirection';
import { FxConversionType } from './FxConversionType';

export interface TO_Forward  {
	 tradeId: string;
	 counterparty: string;
	 portfolioName: string;
	 notional: number;
	 direction: TradeDirection;
	 expiryDate: Date;
	 fixingCalendar: string;
	 paymentCalendar: string;
	 spotLag: string;
	 paymentLag: string;
	 paymentDate: Date;
	 strike: number;
	 assetId: string;
	 paymentCurrency: string;
	 fxFixingId: string;
	 discountCurve: string;
	 fxConversionType: FxConversionType;
	 hedgingSet: string;
}


