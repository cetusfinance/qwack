
import { MultiDimArray } from './MultiDimArray';
import { TO_CorrelationMatrix } from './TO_CorrelationMatrix';
import { Interpolator1DType } from './Interpolator1DType';

export interface TO_CorrelationMatrix  {
	 labelsX: string[];
	 labelsY: string[];
	 correlations: MultiDimArray<number>;
	 isTimeVector: boolean;
	 children: TO_CorrelationMatrix[];
	 times: number[];
	 interpolatorType: Interpolator1DType;
}


