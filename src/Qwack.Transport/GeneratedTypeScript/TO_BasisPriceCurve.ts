
import { TO_PriceCurve } from './TO_PriceCurve';
import { PriceCurveType } from './PriceCurveType';
import { TO_Instrument } from './TO_Instrument';
import { TO_IrCurve } from './TO_IrCurve';
import { CommodityUnits } from './CommodityUnits';

export interface TO_BasisPriceCurve  {
	 baseCurve: TO_PriceCurve;
	 curve: TO_PriceCurve;
	 buildDate: Date;
	 name: string;
	 assetId: string;
	 currency: string;
	 curveType: PriceCurveType;
	 instruments: TO_Instrument[];
	 pillars: Date[];
	 pillarLabels: string[];
	 discountCurve: TO_IrCurve;
	 spotLag: string;
	 spotCalendar: string;
	 units: CommodityUnits;
}


