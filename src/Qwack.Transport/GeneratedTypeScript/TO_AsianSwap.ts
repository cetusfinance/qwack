
import { TradeDirection } from './TradeDirection';
import { RollType } from './RollType';
import { FxConversionType } from './FxConversionType';

export interface TO_AsianSwap  {
	 tradeId: string;
	 counterparty: string;
	 portfolioName: string;
	 notional: number;
	 direction: TradeDirection;
	 averageStartDate: Date;
	 averageEndDate: Date;
	 fixingDates: Date[];
	 fixingCalendar: string;
	 paymentCalendar: string;
	 spotLag: string;
	 spotLagRollType: RollType;
	 paymentLag: string;
	 paymentLagRollType: RollType;
	 paymentDate: Date;
	 strike: number;
	 assetId: string;
	 assetFixingId: string;
	 fxFixingId: string;
	 fxFixingDates: Date[];
	 paymentCurrency: string;
	 fxConversionType: FxConversionType;
	 discountCurve: string;
	 hedgingSet: string;
}


