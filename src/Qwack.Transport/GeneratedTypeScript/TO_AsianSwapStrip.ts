
import { TO_AsianSwap } from './TO_AsianSwap';

export interface TO_AsianSwapStrip  {
	 tradeId: string;
	 counterparty: string;
	 portfolioName: string;
	 swaplets: TO_AsianSwap[];
	 hedgingSet: string;
}


