
import { PriceCurveType } from './PriceCurveType';
import { CommodityUnits } from './CommodityUnits';

export interface TO_BasicPriceCurve  {
	 curveType: PriceCurveType;
	 buildDate: Date;
	 name: string;
	 assetId: string;
	 spotLag: string;
	 spotCalendar: string;
	 pillarDates: Date[];
	 prices: number[];
	 pillarLabels: string[];
	 currency: string;
	 collateralSpec: string;
	 units: CommodityUnits;
}


