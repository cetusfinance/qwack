
import { DayCountBasis } from './DayCountBasis';
import { TO_Interpolator1d } from './TO_Interpolator1d';

export interface TO_HazzardCurve  {
	 constantPD: number;
	 originDate: Date;
	 basis: DayCountBasis;
	 hazzardCurve: TO_Interpolator1d;
}


