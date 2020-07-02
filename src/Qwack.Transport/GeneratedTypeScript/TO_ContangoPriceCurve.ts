
import { DayCountBasis } from './DayCountBasis';
import { CommodityUnits } from './CommodityUnits';

export interface TO_ContangoPriceCurve  {
	 buildDate: Date;
	 name: string;
	 assetId: string;
	 spotLag: string;
	 spotCalendar: string;
	 spot: number;
	 spotDate: Date;
	 contangos: number[];
	 basis: DayCountBasis;
	 pillarLabels: string[];
	 pillarDates: Date[];
	 currency: string;
	 units: CommodityUnits;
}


