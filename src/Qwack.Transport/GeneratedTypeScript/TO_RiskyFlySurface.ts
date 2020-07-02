
import { StrikeType } from './StrikeType';
import { Interpolator1DType } from './Interpolator1DType';
import { MultiDimArray } from './MultiDimArray';
import { DayCountBasis } from './DayCountBasis';
import { WingQuoteType } from './WingQuoteType';
import { AtmVolType } from './AtmVolType';

export interface TO_RiskyFlySurface  {
	 originDate: Date;
	 name: string;
	 currency: string;
	 assetId: string;
	 overrideSpotLag: string;
	 strikes: number[];
	 strikeType: StrikeType;
	 strikeInterpolatorType: Interpolator1DType;
	 timeInterpolatorType: Interpolator1DType;
	 volatilities: MultiDimArray<number>;
	 expiries: Date[];
	 pillarLabels: string[];
	 timeBasis: DayCountBasis;
	 flatDeltaSmileInExtreme: boolean;
	 flatDeltaPoint: number;
	 riskies: MultiDimArray<number>;
	 flies: MultiDimArray<number>;
	 atMs: number[];
	 wingDeltas: number[];
	 forwards: number[];
	 wingQuoteType: WingQuoteType;
	 atmVolType: AtmVolType;
}


