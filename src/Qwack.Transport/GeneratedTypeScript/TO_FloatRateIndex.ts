
import { DayCountBasis } from './DayCountBasis';
import { RollType } from './RollType';

export interface TO_FloatRateIndex  {
	 dayCountBasis: DayCountBasis;
	 dayCountBasisFixed: DayCountBasis;
	 resetTenor: string;
	 resetTenorFixed: string;
	 holidayCalendars: string;
	 rollConvention: RollType;
	 currency: string;
	 fixingOffset: string;
}


