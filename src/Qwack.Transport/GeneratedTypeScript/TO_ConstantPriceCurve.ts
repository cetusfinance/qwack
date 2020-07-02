
import { CommodityUnits } from './CommodityUnits';

export interface TO_ConstantPriceCurve  {
	 buildDate: Date;
	 price: number;
	 currency: string;
	 assetId: string;
	 name: string;
	 units: CommodityUnits;
}


