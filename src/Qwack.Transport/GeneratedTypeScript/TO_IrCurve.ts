
import { Interpolator1DType } from './Interpolator1DType';
import { RateType } from './RateType';
import { DayCountBasis } from './DayCountBasis';

export interface TO_IrCurve  {
	 pillars: Date[];
	 rates: number[];
	 buildDate: Date;
	 name: string;
	 interpKind: Interpolator1DType;
	 ccy: string;
	 collateralSpec: string;
	 rateStorageType: RateType;
	 basis: DayCountBasis;
}


