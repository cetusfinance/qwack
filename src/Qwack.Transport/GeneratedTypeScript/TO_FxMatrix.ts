
import { TO_FxPair } from './TO_FxPair';

export interface TO_FxMatrix  {
	 baseCurrency: string;
	 buildDate: Date;
	 fxPairDefinitions: TO_FxPair[];
	 discountCurveMap: { [key: string]: string; };
	 spotRates: { [key: string]: number; };
}


