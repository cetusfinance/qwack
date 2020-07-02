
import { Interpolator1DType } from './Interpolator1DType';

export interface TO_Interpolator1d  {
	 xs: number[];
	 ys: number[];
	 type: Interpolator1DType;
	 isSorted: boolean;
	 noCopy: boolean;
}


